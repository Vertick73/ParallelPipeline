using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vetinari.Core
{
    public abstract class ParseContainerBase : IDisposable
    {
        protected readonly CancellationTokenSource Cts;
        protected readonly CancellationToken Ctx;
        protected readonly int ThreadsCount;
        public ParseContainerBase Prev;
        protected List<Task> Workers = new();

        protected ParseContainerBase(int threadsCount, CancellationToken? ctx = null)
        {
            ThreadsCount = threadsCount;

            if (ctx == null) Cts = new CancellationTokenSource();
            Ctx = ctx ?? Cts.Token;
        }

        public virtual void Dispose()
        {
            Prev?.Dispose();
            Cts?.Cancel();
            Task.WhenAll(Workers).Wait();
        }

        public virtual void RunAsync()
        {
        }
    }
}
