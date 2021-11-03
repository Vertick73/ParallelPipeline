using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;

namespace Vetinari.Core
{
    public class VkMultiThreadedPipeline<TIn, TOut> : MultiThreadedPipeline<TIn, TOut>
    {
        public VkMultiThreadedPipeline(int queueLength, int threadCount, Func<TIn, Task<IEnumerable<TOut>>> parseFunc,
            CancellationToken? token = null) : base(queueLength, threadCount, parseFunc, token)
        {
        }

        public IVkApi VkApi { get; set; }

        public VkMultiThreadedPipeline<TOut, TNew> AddNext<TNew>(int queueLength, int threadCount,
            Func<TOut, Task<IEnumerable<TNew>>> func, VkMultiThreadedPipeline<TOut, TNew> next = null,
            IVkApi vkApi = null)
        {
            vkApi ??= VkApi;
            next ??= new VkMultiThreadedPipeline<TOut, TNew>(queueLength, threadCount, func)
            {
                Prev = this,
                ResTask = ResTask,
                VkApi = vkApi
            };
            Output = next.Input;
            return next;
        }
    }
}
