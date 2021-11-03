using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VkNet.Exception;

namespace Vetinari.Core
{
    public class MultiThreadedPipeline<TIn, TOut> : MultiThreadedPipelineBase
    {
        private readonly Func<TIn, Task<IEnumerable<TOut>>> _parseFunc;
        private readonly int _threadsCount;
        private readonly List<Task> _workers = new();
        public readonly Channel<TIn> Input;
        private Task _autoCompletionTask;
        private Task _outWriter;
        private CancellationTokenSource _outWriterCts;
        public Channel<TOut> Output;

        public MultiThreadedPipeline(int queueLength, int threadCount, Func<TIn, Task<IEnumerable<TOut>>> parseFunc,
            CancellationToken? token = null) : base(token)
        {
            _threadsCount = threadCount;
            Input = Channel.CreateBounded<TIn>(queueLength);
            _parseFunc = parseFunc;
        }

        public MultiThreadedPipeline<TIn, TOut> SetInput(ICollection<TIn> input)
        {
            _workers.Add(ReadDataAsync(input, Token));
            return this;
        }

        public MultiThreadedPipeline<TIn, TOut> SetOutput(ICollection<TOut> output)
        {
            Output = Channel.CreateUnbounded<TOut>();
            _outWriterCts = new CancellationTokenSource();
            _outWriter = WriteDataAsync(output, _outWriterCts.Token);
            return this;
        }

        public Task Start()
        {
            RunAsync();
            return _autoCompletionTask;
        }

        private async Task ReadDataAsync(ICollection<TIn> inputData, CancellationToken token)
        {
            var writer = Input.Writer;
            try
            {
                foreach (var data in inputData) await writer.WriteAsync(data, token).ConfigureAwait(false);
                writer.TryComplete();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task WriteDataAsync(ICollection<TOut> inputData, CancellationToken token)
        {
            var reader = Output.Reader;
            try
            {
                while (true)
                {
                    var data = await reader.ReadAsync(token).ConfigureAwait(false);
                    inputData.Add(data);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public MultiThreadedPipeline<TOut, TNew> AddNext<TNew>(int queueLength, int threadCount,
            Func<TOut, Task<IEnumerable<TNew>>> func, MultiThreadedPipeline<TOut, TNew> next = null)
        {
            next ??= new MultiThreadedPipeline<TOut, TNew>(queueLength, threadCount, func)
            {
                Prev = this,
                ResTask = ResTask
            };
            Output = next.Input;
            return next;
        }

        public override void RunAsync(bool autoCompletion = true)
        {
            Prev?.RunAsync(autoCompletion);
            for (var i = 0; i < _threadsCount; i++)
            {
                var reader = Input.Reader;
                var writer = Output.Writer;
                _workers.Add(ParseWorkerAsync(reader, writer, Token));
            }

            if (autoCompletion) _autoCompletionTask = CompletionAsync(_workers);
        }

        private async Task CompletionAsync(ICollection<Task> task)
        {
            await Input.Reader.Completion;
            Cts.Cancel();
            await Task.WhenAll(task);
            Output.Writer.TryComplete();
            if (_outWriter != null)
            {
                await Output.Reader.Completion;
                _outWriterCts.Cancel();
                await _outWriter;
            }
        }

        public async Task ParseWorkerAsync(ChannelReader<TIn> reader, ChannelWriter<TOut> writer,
            CancellationToken token)
        {
            try
            {
                while (true)
                {
                    var input = await reader.ReadAsync(token).ConfigureAwait(false);
                    var results = await _parseFunc(input).ConfigureAwait(false);
                    foreach (var result in results) await writer.WriteAsync(result);
                }
            }
            catch (OperationCanceledException)
            {
            }

            catch (RateLimitReachedException ex)
            {
                ResTask.SetException(ex);
            }

            catch (ChannelClosedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
