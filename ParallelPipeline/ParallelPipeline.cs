using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public class ParallelPipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        private readonly int _threadsCount;
        private readonly List<Task> _workers = new();

        public ParallelPipeline(Func<TIn, Task<IEnumerable<TOut>>> func, int queueLength, int threadCount,
            IPipelineOutput<TIn> prev = null) : this(queueLength, threadCount, prev)
        {
            StepFuncIEnumerable = func;
        }

        public ParallelPipeline(Func<TIn, Task<TOut>> func, int queueLength, int threadCount,
            IPipelineOutput<TIn> prev = null) : this(queueLength, threadCount, prev)
        {
            StepFunc = func;
        }

        private ParallelPipeline(int queueLength, int threadCount, IPipelineOutput<TIn> prev = null)
        {
            Cts = new CancellationTokenSource();
            _threadsCount = threadCount;
            Input = Channel.CreateBounded<TIn>(queueLength);
            if (prev != null) prev.Output = Input;
        }

        public virtual Task Run()
        {
            for (var i = 0; i < _threadsCount; i++)
            {
                var reader = Input.Reader;
                var writer = Output.Writer;
                _workers.Add(WorkerAsync(reader, writer, Cts.Token));
            }

            return CompletionAsync(_workers);
        }

        public CancellationTokenSource Cts { get; }

        public Channel<TIn> Input { get; set; }
        public Channel<TOut> Output { get; set; }
        public Func<TIn, Task<TOut>> StepFunc { get; }
        public Func<TIn, Task<IEnumerable<TOut>>> StepFuncIEnumerable { get; }

        private async Task CompletionAsync(ICollection<Task> task)
        {
            await Input.Reader.Completion.ConfigureAwait(false);
            Cts.Cancel();
            await Task.WhenAll(task).ConfigureAwait(false);
            Output.Writer.TryComplete();
        }

        protected virtual async Task WorkerAsync(ChannelReader<TIn> reader, ChannelWriter<TOut> writer,
            CancellationToken token)
        {
            try
            {
                if (StepFuncIEnumerable != null) //1:N
                    while (true)
                    {
                        var input = await reader.ReadAsync(token).ConfigureAwait(false);
                        var results = await StepFuncIEnumerable(input).ConfigureAwait(false);
                        foreach (var result in results) await writer.WriteAsync(result);
                    }

                if (StepFunc != null) //1:1
                    while (true)
                    {
                        var input = await reader.ReadAsync(token).ConfigureAwait(false);
                        var result = await StepFunc(input).ConfigureAwait(false);
                        if (result != null) await writer.WriteAsync(result).ConfigureAwait(false);
                    }
            }
            catch (OperationCanceledException)
            {
            }

            catch (ChannelClosedException)
            {
            }
        }
    }
}
