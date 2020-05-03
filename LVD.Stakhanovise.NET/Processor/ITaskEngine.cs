using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
   public interface ITaskEngine : IDisposable
   {
      Task StartAsync();

      Task StopAync();

      void ScanAssemblies(params Assembly[] assemblies);

      IEnumerable<ITaskWorker> Workers { get; }

      ITaskPoller TaskPoller { get; }

      bool IsRunning { get; }
   }
}
