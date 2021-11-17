using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParallelPipeline.examples.ProducerConsumerExample
{
    internal class Program
    {
        private static async Task Main()
        {
            var pipeline = new ParallelPipelineBuilder<int, string>();
            pipeline.AddStep(x => Enumerable.Range(0, x), 2, 2)
                .AddStepAsync(WriteAsync, 4, 2)
                .AddStep(GetRes, 7, 1);
            var outputChannel = Channel.CreateBounded<string>(1);
            pipeline.SetOutputChanel(outputChannel);
            var inputWriter = pipeline.GetInputWriter();
            var producer = Producer(inputWriter);
            var consumer = Consumer(outputChannel.Reader);
            var start = pipeline.Start();
            await consumer;
        }

        private static async Task Producer(ChannelWriter<int> writer)
        {
            for (var i = 0; i < 10; i++) await writer.WriteAsync(i);
            writer.Complete();
        }

        private static async Task Consumer(ChannelReader<string> reader)
        {
            try
            {
                while (true)
                {
                    var res = await reader.ReadAsync();
                    Console.WriteLine(res);
                    await Task.Delay(100);
                }
            }
            catch (ChannelClosedException e)
            {
                Console.WriteLine("Read completed");
            }
        }

        private static string GetRes(double x)
        {
            return $"res: {x:F1}";
        }

        private static async Task<double> WriteAsync(int data)
        {
            var res = Math.Pow(data, 2);
            using (var sw = new StreamWriter($"{data}.txt"))
            {
                await sw.WriteAsync(res.ToString());
            }

            return res;
        }
    }
}
