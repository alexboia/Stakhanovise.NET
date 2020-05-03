using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
   public interface ITaskResultQueue : IDisposable
   {
      Task EnqueueResultAsync(QueueTask queuedTask, TaskExecutionResult executionResult);
   }
}
