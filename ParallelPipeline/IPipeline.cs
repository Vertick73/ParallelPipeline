using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public interface IPipeline<TIn, TOut> : IPipelineBase, IPipelineInput<TIn>, IPipelineOutput<TOut>
    {
        public Func<TIn, Task<TOut>> StepFuncAsync { get; }
        public Func<TIn, Task<IEnumerable<TOut>>> StepFuncIEnumerableAsync { get; }
    }

    public interface IPipelineInput<TIn>
    {
        public Channel<TIn> Input { get; set; }
    }

    public interface IPipelineOutput<TOut>
    {
        public Channel<TOut> Output { get; set; }
    }

    public interface IPipelineBase
    {
        public CancellationTokenSource Cts { get; }
        public Task Run();
    }
}
