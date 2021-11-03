using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vetinari.Core
{
    public abstract class MultiThreadedPipelineBase : IDisposable
    {
        protected readonly CancellationTokenSource Cts;
        protected readonly CancellationToken Token;
        private bool _disposed;
        public MultiThreadedPipelineBase Prev;
        public TaskCompletionSource ResTask;
        protected List<Task> Workers = new();

        protected MultiThreadedPipelineBase(CancellationToken? token = null)
        {
            if (token == null) Cts = new CancellationTokenSource();
            Token = token ?? Cts.Token;
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

        public virtual void RunAsync(bool autoCompletion)
        {
        }
    }
}
