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
using System.Text.RegularExpressions;

namespace LVD.Stakhanovise.NET.Model
{
	public class QueuedTaskResult : IQueuedTaskResult, IEquatable<QueuedTaskResult>
	{
		public QueuedTaskResult ()
		{
			return;
		}

		public QueuedTaskResult ( IQueuedTask task )
		{
			if ( task == null )
				throw new ArgumentNullException( nameof( task ) );

			Id = task.Id;
			Type = task.Type;
			Payload = task.Payload;
			Status = QueuedTaskStatus.Unprocessed;
			Source = task.Source;
			Priority = task.Priority;
			PostedAtTs = task.PostedAtTs;
			ProcessingTimeMilliseconds = 0;
		}

		public QueuedTaskInfo UdpateFromExecutionResult ( TaskExecutionResult result )
		{
			QueuedTaskInfo repostTask = null;

			//Task processing lifecycle ends with one of these statuses
			//	Hence, if the result reaches one of these points, 
			//	no more updates can be performed
			if ( Status == QueuedTaskStatus.Fatal 
				|| Status == QueuedTaskStatus.Cancelled 
				|| Status == QueuedTaskStatus.Processed )
				throw new InvalidOperationException( $"A result with {Status} status can no longer be updated" );

			if ( result.ExecutedSuccessfully )
				Processed( result );
			else if ( result.ExecutionFailed )
				repostTask = HadError( result );
			else if ( result.ExecutionCancelled )
				Cancelled( result );

			return repostTask;
		}

		private void Processed ( TaskExecutionResult result )
		{
			LastError = null;
			LastErrorIsRecoverable = false;
			ProcessingFinalizedAtTs = DateTimeOffset.UtcNow;
			ProcessingTimeMilliseconds = result.ProcessingTimeMilliseconds;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
			Status = QueuedTaskStatus.Processed;
		}

		private QueuedTaskInfo HadError ( TaskExecutionResult result )
		{
			LastError = result.Error;
			LastErrorIsRecoverable = result.IsRecoverable;
			ErrorCount += 1;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			//If the error is recoverable, work out the status transition 
			//	from the current error count
			if ( result.IsRecoverable )
			{
				if ( ErrorCount > result.FaultErrorThresholdCount + 1 )
					Status = QueuedTaskStatus.Fatal;
				else if ( ErrorCount > result.FaultErrorThresholdCount )
					Status = QueuedTaskStatus.Faulted;
				else
					Status = QueuedTaskStatus.Error;
			}
			else
				//Otherwise, set it to fatal
				Status = QueuedTaskStatus.Fatal;

			//If status is not fatal, compute the information 
			//	necessary to retry task execution
			if ( Status != QueuedTaskStatus.Fatal )
			{
				return new QueuedTaskInfo()
				{
					Id = Id,
					Payload = Payload,
					Type = Type,
					Source = Source,
					Priority = Priority,
					LockedUntilTs = result.RetryAt
				};
			}
			else
				return null;
		}

		private void Cancelled ( TaskExecutionResult result )
		{
			Status = QueuedTaskStatus.Cancelled;

			ProcessingFinalizedAtTs = DateTimeOffset.UtcNow;
			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
		}

		public bool Equals ( QueuedTaskResult other )
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

		public string Type { get; set; }

		public string Source { get; set; }

		public object Payload { get; set; }

		public QueuedTaskStatus Status { get; set; }

		public int Priority { get; set; }

		public long ProcessingTimeMilliseconds { get; set; }

		public QueuedTaskError LastError { get; set; }

		public bool LastErrorIsRecoverable { get; set; }

		public int ErrorCount { get; set; }

		public DateTimeOffset PostedAtTs { get; set; }

		public DateTimeOffset? FirstProcessingAttemptedAtTs { get; set; }

		public DateTimeOffset? LastProcessingAttemptedAtTs { get; set; }

		public DateTimeOffset? ProcessingFinalizedAtTs { get; set; }
	}
}
