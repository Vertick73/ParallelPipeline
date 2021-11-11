using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public class ParallelPipelineStep<TIn, TOut> //split, merge
    {
        protected ParallelPipeline<TIn, TOut> _parallelPipeline;
        protected ICollection<IPipelineBase> _steps;

        public ParallelPipelineStep(Func<TIn, Task<IEnumerable<TOut>>> func, int queueLength, int threadCount)
        {
            _steps = new List<IPipelineBase>();
            _parallelPipeline = new ParallelPipeline<TIn, TOut>(func, queueLength, threadCount);
            _steps.Add(_parallelPipeline);
        }

        public ParallelPipelineStep(ICollection<IPipelineBase> steps, ParallelPipeline<TIn, TOut> parallelPipeline)
        {
            _steps = steps;
            _parallelPipeline = parallelPipeline;
        }

        public ParallelPipelineBuilderBase PipelineBuilder { get; init; }

        public virtual ParallelPipelineStep<TOut, TNew> AddStepAsync<TNew>(
            Func<TOut, Task<IEnumerable<TNew>>> funcAsync,
            int queueLength, int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(funcAsync, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next) { PipelineBuilder = PipelineBuilder };
        }

        public virtual ParallelPipelineStep<TOut, TNew> AddStepAsync<TNew>(Func<TOut, Task<TNew>> funcAsync,
            int queueLength,
            int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(funcAsync, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next) { PipelineBuilder = PipelineBuilder };
        }

        public virtual ParallelPipelineStep<TOut, TNew> AddStep<TNew>(Func<TOut, IEnumerable<TNew>> func,
            int queueLength, int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(func, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next) { PipelineBuilder = PipelineBuilder };
        }

        public virtual ParallelPipelineStep<TOut, TNew> AddStep<TNew>(Func<TOut, TNew> func, int queueLength,
            int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(func, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next) { PipelineBuilder = PipelineBuilder };
        }
    }
}
