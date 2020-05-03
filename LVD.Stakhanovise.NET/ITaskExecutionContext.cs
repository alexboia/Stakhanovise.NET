using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET
{
   public interface ITaskExecutionContext
   {
      TValue Get<TValue>(string key);

      void Set<TValue>(string key, TValue value);

      void NotifyTaskCompleted();

      void NotifyTaskErrored(QueueTaskError error, bool isRecoverable);

      QueueTaskStatus TaskStatus { get; }

      TaskExecutionResult Result { get; }

      bool HasResult { get; }
   }
}
