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

        public virtual ParallelPipelineStep<TOut, TNew> AddStep<TNew>(Func<TOut, Task<IEnumerable<TNew>>> func,
            int queueLength, int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(func, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next);
        }

        public virtual ParallelPipelineStep<TOut, TNew> AddStep<TNew>(Func<TOut, Task<TNew>> func, int queueLength,
            int threadCount)
        {
            var next = new ParallelPipeline<TOut, TNew>(func, queueLength, threadCount, _parallelPipeline);
            _steps.Add(next);
            return new ParallelPipelineStep<TOut, TNew>(_steps, next);
        }
    }
}
