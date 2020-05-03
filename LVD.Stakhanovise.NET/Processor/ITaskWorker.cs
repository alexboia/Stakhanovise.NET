using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
   public interface ITaskWorker
   {
      Task StartAsync(params string[] requiredPayloadTypes);

      Task StopAync();

      bool IsRunning { get; }
   }
}
