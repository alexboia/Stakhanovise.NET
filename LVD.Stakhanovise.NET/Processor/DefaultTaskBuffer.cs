using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Processor
{
   public class DefaultTaskBuffer : ITaskBuffer
   {
      private int mCapacity;

      private BlockingCollection<QueueTask> mInnerBuffer;

      private bool mIsDisposed = false;

      public event EventHandler QueuedTaskRetrieved;

      public event EventHandler QueuedTaskAdded;

      public DefaultTaskBuffer(int capacity)
      {
         if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be greater than 0");

         mInnerBuffer = new BlockingCollection<QueueTask>(new ConcurrentQueue<QueueTask>(), capacity);
         mCapacity = capacity;
      }

      private void CheckDisposedOrThrow()
      {
         if (mIsDisposed)
            throw new ObjectDisposedException(nameof(DefaultTaskBuffer), "Cannot reuse a disposed FIFO task buffer");
      }

      private void NotifyQueuedTaskRetrieved()
      {
         EventHandler itemRetrievedHandler = QueuedTaskRetrieved;
         if (itemRetrievedHandler != null)
            itemRetrievedHandler.Invoke(this, EventArgs.Empty);
      }

      private void NotifyQueuedTaskAdded()
      {
         EventHandler itemAddedHandler = QueuedTaskAdded;
         if (itemAddedHandler != null)
            itemAddedHandler.Invoke(this, EventArgs.Empty);
      }

      public bool TryAddNewTask(QueueTask task)
      {
         CheckDisposedOrThrow();

         if (task == null)
            throw new ArgumentNullException(nameof(task));

         if (mInnerBuffer.IsAddingCompleted)
            return false;

         bool isAdded = mInnerBuffer.TryAdd(task);
         if (isAdded)
            NotifyQueuedTaskAdded();

         return isAdded;
      }

      public QueueTask TryGetNextTask()
      {
         CheckDisposedOrThrow();

         QueueTask newTask;
         if (!mInnerBuffer.TryTake(out newTask))
            newTask = null;

         if (newTask != null)
            NotifyQueuedTaskRetrieved();

         return newTask;
      }

      public void CompleteAdding()
      {
         CheckDisposedOrThrow();
         mInnerBuffer.CompleteAdding();
      }

      protected virtual void Dispose(bool disposing)
      {
         if (!mIsDisposed)
         {
            if (disposing)
            {
               mInnerBuffer.Dispose();
               mInnerBuffer = null;
            }

            mIsDisposed = true;
         }
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      public int Count => mInnerBuffer.Count;

      public bool HasTasks => mInnerBuffer.Count > 0;

      public bool IsFull => mInnerBuffer.Count == mCapacity;

      public int Capacity => mCapacity;

      public bool IsCompleted => mInnerBuffer.IsCompleted;
   }
}
