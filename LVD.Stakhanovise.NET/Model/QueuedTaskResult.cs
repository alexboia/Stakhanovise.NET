﻿using System;
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
			Status = QueuedTaskStatus.Processing;
			Priority = task.Priority;
			PostedAt = task.PostedAt;
			PostedAtTs = task.PostedAtTs;
			ProcessingTimeMilliseconds = 0;
		}

		public QueuedTaskInfo UdpateFromExecutionResult ( TaskExecutionResult result )
		{
			QueuedTaskInfo repostTask = null;

			if ( result.ExecutedSuccessfully )
				Processed( result );
			else if ( !result.ExecutionCancelled )
				repostTask = HadError( result );

			return repostTask;
		}

		private void Processed ( TaskExecutionResult result )
		{
			Status = QueuedTaskStatus.Processed;
			ProcessingFinalizedAtTs = DateTimeOffset.UtcNow;
			ProcessingTimeMilliseconds = result.ProcessingTimeMilliseconds;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;
		}

		private QueuedTaskInfo HadError ( TaskExecutionResult result )
		{
			LastError = result.Error;
			LastErrorIsRecoverable = result.IsRecoverable;
			ErrorCount += 1;

			if ( !FirstProcessingAttemptedAtTs.HasValue )
				FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow;

			if ( Status != QueuedTaskStatus.Fatal &&
				Status != QueuedTaskStatus.Faulted )
				Status = QueuedTaskStatus.Error;

			if ( ErrorCount > result.FaultErrorThresholdCount + 1 )
				Status = QueuedTaskStatus.Fatal;
			else if ( ErrorCount > result.FaultErrorThresholdCount )
				Status = QueuedTaskStatus.Faulted;
			else
				Status = QueuedTaskStatus.Error;

			if ( result.IsRecoverable && Status != QueuedTaskStatus.Fatal )
			{
				return new QueuedTaskInfo()
				{
					Id = Id,
					Payload = Payload,
					Type = Type,
					Source = Source,
					Priority = Priority,
					LockedUntil = result.RetryAtTicks
				};
			}
			else
				return null;
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

		public long PostedAt { get; set; }

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
