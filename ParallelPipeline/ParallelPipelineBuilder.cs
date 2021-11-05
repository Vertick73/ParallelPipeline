using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public class ParallelPipelineBuilder<TIn, TLastOut> : ParallelPipelineBuilderBase //start
    {
        private IPipelineInput<TIn> _input;
        private IPipelineOutput<TLastOut> _output;

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
            Steps = new List<IPipelineBase>();
            _input = input;
            Steps.Add(input);
            return new ParallelPipelineStep<TIn, TNew>(Steps, input) { PipelineBuilder = this };
        }

        public ChannelWriter<TIn> GetInputWriter()
        {
            return _input.Input.Writer;
        }

        public void SetOutputChanel(Channel<TLastOut> outChannel)
        {
            try
            {
                _output = (IPipelineOutput<TLastOut>)Steps.Last();
                _output.Output = outChannel;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
