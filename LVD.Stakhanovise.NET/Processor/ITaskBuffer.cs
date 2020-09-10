using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Processor
{
   public interface ITaskBuffer : IDisposable
   {
      event EventHandler QueuedTaskRetrieved;

      event EventHandler QueuedTaskAdded;

      QueuedTask TryGetNextTask();

      bool TryAddNewTask(QueuedTask task);

      void CompleteAdding();

      bool HasTasks { get; }

      bool IsFull { get; }

      int Capacity { get; }

      int Count { get; }

      bool IsCompleted { get; }
   }
}
