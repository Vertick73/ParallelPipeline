using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelPipeline.examples.UsageExample
{
    internal class Program
    {
        private static async Task Main()
        {
            var pipeline = new ParallelPipelineBuilder<int, string>();
            pipeline.AddStep(x => Enumerable.Range(0, x), 5, 2)
                .AddStepAsync(WriteAsync, 4, 2)
                .AddStep(GetRes, 7, 1);
            var results = await pipeline.Start(10);
            foreach (var res in results) Console.WriteLine(res);
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
