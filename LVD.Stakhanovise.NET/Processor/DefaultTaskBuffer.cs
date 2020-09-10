// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
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

      private BlockingCollection<QueuedTask> mInnerBuffer;

      private bool mIsDisposed = false;

      public event EventHandler QueuedTaskRetrieved;

      public event EventHandler QueuedTaskAdded;

      public DefaultTaskBuffer(int capacity)
      {
         if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be greater than 0");

         mInnerBuffer = new BlockingCollection<QueuedTask>(new ConcurrentQueue<QueuedTask>(), capacity);
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

      public bool TryAddNewTask(QueuedTask task)
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

      public QueuedTask TryGetNextTask()
      {
         CheckDisposedOrThrow();

         QueuedTask newTask;
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
