﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParallelPipeline
{
    public abstract class ParallelPipelineBuilderBase:IDisposable
    {
        public ICollection<IPipelineBase> Steps { get; protected set; }

        public void Dispose()
        {
            foreach (var step in Steps)
            {
                step.Cts.Cancel();
            }
        }

        public virtual Task Start()
        {
            var tasks = new List<Task>();
            foreach (var step in Steps) tasks.Add(step.Run());
            return Task.WhenAll(tasks);
        }
    }
}
