using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace LVD.Stakhanovise.NET.Executors
{
   public interface ITaskExecutorRegistry
   {
      void ScanAssemblies(params Assembly[] assemblies);

      ITaskExecutor<TPayload> ResolveExecutor<TPayload>();

      ITaskExecutor ResolveExecutor(Type payloadType);

      Type ResolvePayloadType(string type);

      IEnumerable<Type> DetectedPayloadTypes { get; }
   }
}
