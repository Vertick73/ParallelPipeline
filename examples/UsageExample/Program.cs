using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelPipeline.examples.UsageExample
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var pipeline = new ParallelPipelineBuilder<int, string>();
            pipeline.AddStep(async x =>
                {
                    await Task.Delay(10);
                    return Enumerable.Range(0, x);
                }, 5, 2)
                .AddStep(WriteAsync, 4, 2)
                .AddStep(GetRes, 7, 1);
            var results = await pipeline.Start(10);
            foreach (var res in results) Console.WriteLine(res);
        }

        private static async Task<string> GetRes(double x)
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
