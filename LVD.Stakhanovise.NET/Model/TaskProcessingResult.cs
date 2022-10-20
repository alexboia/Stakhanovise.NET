using LVD.Stakhanovise.NET.Queue;
using System;

namespace LVD.Stakhanovise.NET.Model
{
	public sealed class TaskProcessingResult
	{
		public TaskProcessingResult( IQueuedTaskToken queuedTaskToken,
			TaskExecutionResult taskExecutionResult )
		{
			QueuedTaskToken = queuedTaskToken
				?? throw new ArgumentNullException( nameof( queuedTaskToken ) );
			ExecutionResult = taskExecutionResult
				?? throw new ArgumentNullException( nameof( taskExecutionResult ) );
		}

		public IQueuedTaskToken QueuedTaskToken
		{
			get; private set;
		}

		public TaskExecutionResult ExecutionResult
		{
			get; private set;
		}

		public long ProcessingTimeMilliseconds
			=> ExecutionResult.ProcessingTimeMilliseconds;

		public bool ExecutedSuccessfully
			=> ExecutionResult.ExecutedSuccessfully;

		public bool ExecutionFailed
			=> ExecutionResult.ExecutionFailed;

		public bool ExecutionCancelled
			=> ExecutionResult.ExecutionCancelled;

		public QueuedTaskError Error
			=> ExecutionResult.Error;
	}
}
