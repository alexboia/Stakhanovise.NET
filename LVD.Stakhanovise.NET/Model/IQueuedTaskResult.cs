using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public interface IQueuedTaskResult
	{
		QueuedTaskInfo UdpateFromExecutionResult ( TaskExecutionResult result );


		Guid Id { get; }

		string Type { get; }

		string Source { get; }

		object Payload { get; }

		QueuedTaskStatus Status { get; }

		int Priority { get; }

		long ProcessingTimeMilliseconds { get; }

		QueuedTaskError LastError { get; }

		bool LastErrorIsRecoverable { get; }

		int ErrorCount { get; }

		DateTimeOffset PostedAtTs { get; }

		DateTimeOffset? FirstProcessingAttemptedAtTs { get; }

		DateTimeOffset? LastProcessingAttemptedAtTs { get; }

		DateTimeOffset? ProcessingFinalizedAtTs { get; }
	}
}
