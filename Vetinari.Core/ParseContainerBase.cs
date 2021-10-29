using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VkNet.Abstractions;

namespace Vetinari.Core
{
    public abstract class ParseContainerBase : IDisposable
    {
        protected readonly CancellationTokenSource Cts;
        protected readonly CancellationToken Ctx;
        protected readonly int ThreadsCount;
        private bool _disposed;
        public bool IsCompleted;
        public bool IsLastChain = false;
        public ParseContainerBase Prev;
        public int RequestsCompleted;
        public int RequestsTotal;
        public TaskCompletionSource ResTask;
        public IVkApi Vk;
        protected List<Task> Workers = new();

        protected ParseContainerBase(int threadsCount, CancellationToken? ctx = null)
        {
            ThreadsCount = threadsCount;
            if (ctx == null) Cts = new CancellationTokenSource();
            Ctx = ctx ?? Cts.Token;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Prev?.Dispose();
                Cts?.Cancel();
                Task.WhenAll(Workers).Wait();
            }

            _disposed = true;
        }

        public virtual void RunAsync()
        {
        }
    }
}
