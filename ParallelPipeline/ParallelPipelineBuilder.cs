using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public class ParallelPipelineBuilder<TIn, TLastOut> : ParallelPipelineBuilderBase
    {
        private IPipelineInput<TIn> _input;
        private IPipelineOutput<TLastOut> _output;

        public ParallelPipelineStep<TIn, TNew> AddStepAsync<TNew>(Func<TIn, Task<IEnumerable<TNew>>> funcAsync,
            int queueLength,
            int threadCount)
        {
            var input = new ParallelPipeline<TIn, TNew>(funcAsync, queueLength, threadCount);
            return AddStepBase(input);
        }

        public ParallelPipelineStep<TIn, TNew> AddStepAsync<TNew>(Func<TIn, Task<TNew>> funcAsync, int queueLength,
            int threadCount)
        {
            var input = new ParallelPipeline<TIn, TNew>(funcAsync, queueLength, threadCount);
            return AddStepBase(input);
        }

        public ParallelPipelineStep<TIn, TNew> AddStep<TNew>(Func<TIn, IEnumerable<TNew>> func, int queueLength,
            int threadCount)
        {
            var input = new ParallelPipeline<TIn, TNew>(func, queueLength, threadCount);
            return AddStepBase(input);
        }

        public ParallelPipelineStep<TIn, TNew> AddStep<TNew>(Func<TIn, TNew> func, int queueLength,
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

        public async Task<ICollection<TLastOut>> Start(TIn input)
        {
            _output ??= (IPipelineOutput<TLastOut>)Steps.Last();
            _output.Output = Channel.CreateUnbounded<TLastOut>();
            var writer = _input.Input.Writer;
            await writer.WriteAsync(input).ConfigureAwait(false);
            writer.Complete();
            await Start().ConfigureAwait(false);
            return await GetRes().ConfigureAwait(false);
        }

        public async Task<ICollection<TLastOut>> Start(IEnumerable<TIn> input)
        {
            _output ??= (IPipelineOutput<TLastOut>)Steps.Last();
            _output.Output = Channel.CreateUnbounded<TLastOut>();
            var startTask = Start();
            var writer = _input.Input.Writer;
            foreach (var data in input) await writer.WriteAsync(data).ConfigureAwait(false);
            writer.Complete();
            await startTask;
            return await GetRes().ConfigureAwait(false);
        }

        public async Task<ICollection<TLastOut>> GetRes()
        {
            var reader = _output.Output.Reader;
            var allData = reader.ReadAllAsync();
            var res = new List<TLastOut>(reader.Count);
            await foreach (var data in allData) res.Add(data);
            return res;
        }
    }
}
