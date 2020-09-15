using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public interface IQueuedTask
	{
		Guid Id { get; }

		long LockHandleId { get; }

		string Type { get; set; }

		string Source { get; }

		object Payload { get; }

		QueuedTaskStatus Status { get; }

		int Priority { get; set; }

		long LockedUntil { get; }

		long ProcessingTimeMilliseconds { get; }

		QueuedTaskError LastError { get; }

		bool LastErrorIsRecoverable { get; }

		int ErrorCount { get; }

		DateTimeOffset PostedAt { get; }

		DateTimeOffset RepostedAt { get; }

		DateTimeOffset? FirstProcessingAttemptedAt { get; }

		DateTimeOffset? LastProcessingAttemptedAt { get; }

		DateTimeOffset? ProcessingFinalizedAt { get; }
	}
}
