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
	public class QueuedTask : IQueuedTask, IEquatable<QueuedTask>
	{
		public QueuedTask ()
		{
			return;
		}

		public QueuedTask ( Guid taskId )
			: this()
		{
			Id = taskId;
		}

		public virtual void ProcessingStarted ( AbstractTimestamp lockUntil )
		{
			Status = QueuedTaskStatus.Processing;
			LockedUntil = lockUntil.Ticks;
		}

		public virtual void Processed ( long processingTimeMilliseconds )
		{
			Status = QueuedTaskStatus.Processed;
			ProcessingFinalizedAtTs = DateTimeOffset.UtcNow;
			ProcessingTimeMilliseconds = processingTimeMilliseconds;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
		}

		public virtual void Faulted ()
		{
			if ( Status == QueuedTaskStatus.Error )
			{
				Status = QueuedTaskStatus.Faulted;
				LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
				RepostedAtTs = DateTimeOffset.UtcNow;
			}
		}

		public virtual void HadError ( QueuedTaskError error,
			bool isRecoverable,
			int faultErrorThresholdCount,
			AbstractTimestamp retryAt )
		{
			LastError = error;
			LastErrorIsRecoverable = isRecoverable;
			ErrorCount += 1;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
			RepostedAtTs = DateTimeOffset.UtcNow;
			if ( Status != QueuedTaskStatus.Fatal &&
				Status != QueuedTaskStatus.Faulted )
				Status = QueuedTaskStatus.Error;

			if ( ErrorCount >= faultErrorThresholdCount )
			{
				if ( Status == QueuedTaskStatus.Error )
					Faulted();
				else if ( Status == QueuedTaskStatus.Faulted )
					ProcessingFailedPermanently();
			}

			if ( retryAt != null )
				LockedUntil = retryAt.Ticks;
			else
				LockedUntil = 0;
		}

		public virtual void ProcessingFailedPermanently ()
		{
			if ( Status == QueuedTaskStatus.Faulted )
			{
				Status = QueuedTaskStatus.Fatal;
				LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
			}
		}

		public bool Equals ( QueuedTask other )
		{
			if ( other == null )
				return false;

			if ( Id.Equals( Guid.Empty ) && other.Id.Equals( Guid.Empty ) )
				return ReferenceEquals( this, other );

			return Id.Equals( other.Id );
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as QueuedTask );
		}

		public override int GetHashCode ()
		{
			return Id.GetHashCode();
		}

		public Guid Id { get; set; }

		public long LockHandleId { get; set; }

		public string Type { get; set; }

		public string Source { get; set; }

		public object Payload { get; set; }

		public QueuedTaskStatus Status { get; set; }

		public int Priority { get; set; }

		public long PostedAt { get; set; }

		public long LockedUntil { get; set; }

		public long ProcessingTimeMilliseconds { get; set; }

		public QueuedTaskError LastError { get; set; }

		public bool LastErrorIsRecoverable { get; set; }

		public int ErrorCount { get; set; }

		public DateTimeOffset PostedAtTs { get; set; }

		public DateTimeOffset RepostedAtTs { get; set; }

		public DateTimeOffset? FirstProcessingAttemptedAtTs { get; set; }

		public DateTimeOffset? LastProcessingAttemptedAtTs { get; set; }

		public DateTimeOffset? ProcessingFinalizedAtTs { get; set; }
	}
}
