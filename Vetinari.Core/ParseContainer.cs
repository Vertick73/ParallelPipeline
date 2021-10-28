using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Vetinari.Core
{
    public class ParseContainer<TIn, TOut> : ParseContainerBase
    {
        private readonly Channel<TIn> _input;
        private readonly Func<TIn, IEnumerable<TOut>> _parseFunc;

        private readonly List<Task> _workers = new();
        private Channel<TOut> _output;

        public ParseContainer(int queueLength, int threadCount, Func<TIn, IEnumerable<TOut>> parseFunc,
            CancellationToken? ctx = null) : base(threadCount)
        {
            _input = Channel.CreateBounded<TIn>(queueLength);
            _parseFunc = parseFunc;
        }

        public ParseContainer<TIn, TOut> SetInput(IEnumerable<TIn> input)
        {
            _workers.Add(ReadDataAsync(input));
            return this;
        }

        public ParseContainer<TIn, TOut> SetOutput(ICollection<TOut> output)
        {
            _output = Channel.CreateUnbounded<TOut>();
            _workers.Add(WriteDataAsync(output));
            return this;
        }

        public ParseContainer<TIn, TOut> Start()
        {
            RunAsync();
            return this;
        }

        private async Task ReadDataAsync(IEnumerable<TIn> inputData)
        {
            var writer = _input.Writer;
            try
            {
                foreach (var data in inputData)
                {
                    Ctx.ThrowIfCancellationRequested();
                    await writer.WriteAsync(data, Ctx);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task WriteDataAsync(ICollection<TOut> inputData)
        {
            var reader = _output.Reader;
            try
            {
                while (true)
                {
                    Ctx.ThrowIfCancellationRequested();
                    inputData.Add(await reader.ReadAsync(Ctx));
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public ParseContainer<TOut, TNew> AddNext<TNew>(int queueLength, int threadCount,
            Func<TOut, IEnumerable<TNew>> func)
        {
            var t = new ParseContainer<TOut, TNew>(queueLength, threadCount, func, Ctx);
            t.Prev = this;
            _output = t._input;
            return t;
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
                    var input = await reader.ReadAsync(ctx);
                    var results = _parseFunc(input);
                    foreach (var result in results) await writer.WriteAsync(result, ctx);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
