using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Executors
{
   public interface ITaskExecutor<TPayload> : ITaskExecutor
   {
      Task ExecuteAsync(TPayload payload, ITaskExecutionContext executionContext);
   }
}
