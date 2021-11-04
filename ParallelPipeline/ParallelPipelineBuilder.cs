using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public class ParallelPipelineBuilder<TIn, TOut> //start
    {
        private IPipelineInput<TIn> _input;
        private IPipelineOutput<TOut> _output;
        private ICollection<IPipelineBase> _steps;

        public ParallelPipelineStep<TIn, TNew> AddStep<TNew>(Func<TIn, Task<IEnumerable<TNew>>> func, int queueLength,
            int threadCount)
        {
            var input = new ParallelPipeline<TIn, TNew>(func, queueLength, threadCount);
            return AddStepBase(input);
        }

        public ParallelPipelineStep<TIn, TNew> AddStep<TNew>(Func<TIn, Task<TNew>> func, int queueLength,
            int threadCount)
        {
            var input = new ParallelPipeline<TIn, TNew>(func, queueLength, threadCount);
            return AddStepBase(input);
        }

        private ParallelPipelineStep<TIn, TNew> AddStepBase<TNew>(ParallelPipeline<TIn, TNew> input)
        {
            _steps = new List<IPipelineBase>();
            _input = input;
            _steps.Add(input);
            return new ParallelPipelineStep<TIn, TNew>(_steps, input);
        }

        public Task Start()
        {
            var tasks = new List<Task>();
            foreach (var step in _steps) tasks.Add(step.Run());

            return Task.WhenAll(tasks);
        }

        public ChannelWriter<TIn> GetInputWriter()
        {
            return _input.Input.Writer;
        }

        public void SetOutputChanel(Channel<TOut> outChannel)
        {
            try
            {
                _output = (IPipelineOutput<TOut>)_steps.Last();
                _output.Output = outChannel;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
