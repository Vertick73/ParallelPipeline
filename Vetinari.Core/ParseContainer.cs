using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Exception;

namespace Vetinari.Core
{
    public class ParseContainer : ParseContainerBase
    {
        public ParseContainer(IVkApi vkApi) : base(0)
        {
            Vk = vkApi;
        }

        public ParseContainer<TIn, TOut> AddNext<TIn, TOut>(int queueLength, int threadCount,
            Func<TIn, Task<IEnumerable<TOut>>> func)
        {
            ResTask = new TaskCompletionSource();
            return new ParseContainer<TIn, TOut>(queueLength, threadCount, func, Vk) { Prev = this, ResTask = ResTask };
        }
    }

    public class ParseContainer<TIn, TOut> : ParseContainerBase
    {
        private readonly Channel<TIn> _input;
        private readonly Func<TIn, Task<IEnumerable<TOut>>> _parseFunc;
        private readonly List<Task> _workers = new();
        private Channel<TOut> _output;
        private Task _outWriter;

        public ParseContainer(int queueLength, int threadCount, Func<TIn, Task<IEnumerable<TOut>>> parseFunc, IVkApi vk,
            CancellationToken? ctx = null) : base(threadCount, ctx)
        {
            _input = Channel.CreateBounded<TIn>(queueLength);
            _parseFunc = parseFunc;
            Vk = vk;
        }

        public ParseContainer<TIn, TOut> SetInput(ICollection<TIn> input)
        {
            _workers.Add(ReadDataAsync(input));
            return this;
        }

        public ParseContainer<TIn, TOut> SetOutput(ICollection<TOut> output)
        {
            _output = Channel.CreateUnbounded<TOut>();
            _outWriter = WriteDataAsync(output);
            return this;
        }

        public Task Start()
        {
            RunAsync();
            IsLastChain = true;
            return ResTask.Task;
        }

        private async Task ReadDataAsync(ICollection<TIn> inputData)
        {
            var writer = _input.Writer;
            try
            {
                foreach (var data in inputData)
                {
                    Ctx.ThrowIfCancellationRequested();
                    await writer.WriteAsync(data, Ctx).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }

            Prev.IsCompleted = true;
            Prev.RequestsTotal = inputData.Count;
        }

        private async Task WriteDataAsync(ICollection<TOut> inputData)
        {
            var reader = _output.Reader;
            try
            {
                while (true)
                {
                    Ctx.ThrowIfCancellationRequested();
                    inputData.Add(await reader.ReadAsync(Ctx).ConfigureAwait(false));
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public ParseContainer<TOut, TNew> AddNext<TNew>(int queueLength, int threadCount,
            Func<TOut, Task<IEnumerable<TNew>>> func) //where TNew : IEnumerable<TNew>
        {
            var next = new ParseContainer<TOut, TNew>(queueLength, threadCount, func, Vk)
            {
                Prev = this,
                ResTask = ResTask
            };
            _output = next._input;
            return next;
        }

        public override void RunAsync()
        {
            Prev?.RunAsync();
            for (var i = 0; i < ThreadsCount; i++)
            {
                var reader = _input.Reader;
                var writer = _output.Writer;
                _workers.Add(ParseWorkerAsync(reader, writer, Ctx));
            }
        }

        public async Task ParseWorkerAsync(ChannelReader<TIn> reader, ChannelWriter<TOut> writer, CancellationToken ctx)
        {
            try
            {
                while (true)
                {
                    ctx.ThrowIfCancellationRequested();
                    var input = await reader.ReadAsync(ctx).ConfigureAwait(false);
                    var results = await _parseFunc(input).ConfigureAwait(false);
                    foreach (var result in results)
                    {
                        await writer.WriteAsync(result, ctx).ConfigureAwait(false);
                        Interlocked.Add(ref RequestsTotal, 1);
                    }

                    var completed = Interlocked.Add(ref RequestsCompleted, 1);
                    if (Prev.IsCompleted && Prev.RequestsTotal == RequestsCompleted)
                    {
                        IsCompleted = true;
                        if (IsLastChain)
                        {
                            await _outWriter;
                            ResTask.SetResult();
                        }

                        Cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            catch (RateLimitReachedException ex)
            {
                ResTask.SetException(ex);
            }
        }
    }
}
