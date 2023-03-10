using LVD.Stakhanovise.NET.Exceptions;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskProcessor : ITaskProcessor
	{
		private readonly TaskProcessingOptions mOptions;

		private readonly ITaskExecutorResolver mExecutorResolver;

		private readonly ITaskExecutionRetryCalculator mRetryCalculator;

		private readonly IStakhanoviseLogger mLogger;

		public StandardTaskProcessor( TaskProcessingOptions options,
			ITaskExecutorResolver executorResolver,
			ITaskExecutionRetryCalculator retryCalculator,
			IStakhanoviseLogger logger )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mExecutorResolver = executorResolver
				?? throw new ArgumentNullException( nameof( executorResolver ) );
			mRetryCalculator = retryCalculator
				?? throw new ArgumentNullException( nameof( retryCalculator ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		public async Task<TaskProcessingResult> ProcessTaskAsync( TaskExecutionContext executionContext )
		{
			if ( executionContext == null )
				throw new ArgumentNullException( nameof( executionContext ) );

			try
			{
				//Check for cancellation before we start execution
				executionContext.StartTimingExecution();
				executionContext.ThrowIfCancellationRequested();

				//Attempt to resolve and run task executor
				ITaskExecutor taskExecutor;
				if ( ( taskExecutor = ResolveTaskExecutor( executionContext ) ) == null )
					throw new TaskExecutorNotFoundException( executionContext.DequeuedTaskPayloadType );

				mLogger.DebugFormat( "Beginning task execution. Task id = {0}.",
					executionContext.DequeuedTaskId );

				//Execute task
				await taskExecutor.ExecuteAsync( executionContext.DequeuedTaskPayload,
					executionContext );

				mLogger.DebugFormat( "Task execution completed. Task id = {0}.",
					executionContext.DequeuedTaskId );

				//Ensure we have a result - since no exception was thrown 
				//	and no result explicitly set, assume success.
				if ( !executionContext.HasResult )
					executionContext.SetTaskCompleted();
			}
			catch ( OperationCanceledException )
			{
				//User code has observed cancellation request 
				executionContext?.SetCancellationObserved();
			}
			catch ( Exception exc )
			{
				HandleTaskProcessingError( executionContext,
					exc );
			}
			finally
			{
				executionContext.StopTimingExecution();
			}

			return CreateExecutionResult( executionContext );
		}

		private ITaskExecutor ResolveTaskExecutor( TaskExecutionContext executionContext )
		{
			return mExecutorResolver.ResolveExecutor( executionContext
				.DequeuedTask );
		}

		private void HandleTaskProcessingError( TaskExecutionContext executionContext,
			Exception exc )
		{
			bool isRecoverable = mOptions.IsTaskErrorRecoverable(
				executionContext.DequeuedTask,
				exc
			);

			executionContext?.SetTaskErrored(
				new QueuedTaskError( exc ),
				isRecoverable
			);

			mLogger.Error( "Error executing queued task",
				exc );
		}

		private TaskProcessingResult CreateExecutionResult( TaskExecutionContext executionContext )
		{
			DateTimeOffset retryAt = DateTimeOffset.UtcNow;

			//Compute the amount of time to delay task execution
			//	if execution failed
			if ( executionContext.HasResult
				&& executionContext.ExecutionFailed )
				retryAt = ComputeRetryAt( executionContext );

			//TODO: at some point, take not of whether or not the error
			//	(if any, is reported as recoverable and DO NOT flag it for retrial)

			TaskExecutionResult executionResult =
				new TaskExecutionResult( executionContext.ResultInfo,
					executionContext.Duration,
					retryAt,
					mOptions.FaultErrorThresholdCount );

			return new TaskProcessingResult(
				executionContext.DequeuedTaskToken,
				executionResult
			);
		}

		private DateTimeOffset ComputeRetryAt( TaskExecutionContext executionContext )
		{
			return mRetryCalculator.ComputeRetryAt( executionContext
				.DequeuedTaskToken );
		}
	}
}
