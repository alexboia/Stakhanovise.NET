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
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
   public class QueueTask : IEquatable<QueueTask>
   {
      public QueueTask()
      {
         return;
      }

      public QueueTask(Guid taskId)
          : this()
      {
         Id = taskId;
      }

      public virtual void Processed()
      {
         Status = QueueTaskStatus.Processed;
         ProcessingFinalizedAt = DateTimeOffset.Now;

         if (!FirstProcessingAttemptedAt.HasValue)
            FirstProcessingAttemptedAt = DateTimeOffset.Now;

         LastProcessingAttemptedAt = DateTimeOffset.Now;
      }

      public virtual void Faulted()
      {
         if (Status == QueueTaskStatus.Error)
         {
            Status = QueueTaskStatus.Faulted;
            LastProcessingAttemptedAt = DateTimeOffset.Now;
            RepostedAt = DateTimeOffset.Now;
         }
      }

      public virtual void HadError(QueueTaskError error, bool isRecoverable)
      {
         LastError = error;
         LastErrorIsRecoverable = isRecoverable;
         ErrorCount += 1;

         if (!FirstProcessingAttemptedAt.HasValue)
            FirstProcessingAttemptedAt = DateTimeOffset.Now;

         LastProcessingAttemptedAt = DateTimeOffset.Now;
         RepostedAt = DateTimeOffset.Now;
         if (Status != QueueTaskStatus.Fatal &&
             Status != QueueTaskStatus.Faulted)
            Status = QueueTaskStatus.Error;
      }

      public virtual void ProcessingFailedPermanently()
      {
         if (Status == QueueTaskStatus.Faulted)
         {
            Status = QueueTaskStatus.Fatal;
            LastProcessingAttemptedAt = DateTimeOffset.Now;
         }
      }

      public bool Equals(QueueTask other)
      {
         if (other == null)
            return false;

         if (Id.Equals(Guid.Empty) && other.Id.Equals(Guid.Empty))
            return ReferenceEquals(this, other);

         return Id.Equals(other.Id);
      }

      public override bool Equals(object obj)
      {
         return Equals(obj as QueueTask);
      }

      public override int GetHashCode()
      {
         return Id.GetHashCode();
      }

      public Guid Id { get; set; }

      public long LockHandleId { get; set; }

      public string Type { get; set; }

      public string Source { get; set; }

      public object Payload { get; set; }

      public QueueTaskStatus Status { get; set; }

      public int Priority { get; set; }

      public QueueTaskError LastError { get; set; }

      public bool LastErrorIsRecoverable { get; set; }

      public int ErrorCount { get; set; }

      public DateTimeOffset PostedAt { get; set; }

      public DateTimeOffset RepostedAt { get; set; }

      public DateTimeOffset? FirstProcessingAttemptedAt { get; set; }

      public DateTimeOffset? LastProcessingAttemptedAt { get; set; }

      public DateTimeOffset? ProcessingFinalizedAt { get; set; }
   }
}
