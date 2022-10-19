using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;

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

		public async Task<TaskExecutionResult> ProcessTaskAsync( TaskExecutionContext executionContext )
		{
			if ( executionContext == null )
				throw new ArgumentNullException( nameof( executionContext ) );
			
			ITaskExecutor taskExecutor = null;
			IQueuedTask dequeuedTask = ExtractTask( executionContext );

			try
			{
				//Check for cancellation before we start execution
				executionContext.StartTimingExecution();
				executionContext.ThrowIfCancellationRequested();

				//Attempt to resolve and run task executor
				if ( ( taskExecutor = ResolveTaskExecutor( dequeuedTask ) ) != null )
				{
					mLogger.DebugFormat( "Beginning task execution. Task id = {0}.",
						dequeuedTask.Id );

					//Execute task
					await taskExecutor.ExecuteAsync( dequeuedTask.Payload,
						executionContext );

					mLogger.DebugFormat( "Task execution completed. Task id = {0}.",
						dequeuedTask.Id );

					//Ensure we have a result - since no exception was thrown 
					//	and no result explicitly set, assume success.
					if ( !executionContext.HasResult )
						executionContext.SetTaskCompleted();
				}
			}
			catch ( OperationCanceledException )
			{
				//User code has observed cancellation request 
				executionContext?.SetCancellationObserved();
			}
			catch ( Exception exc )
			{
				HandleTaskProcessingError( executionContext, 
					dequeuedTask, 
					exc );
			}
			finally
			{
				executionContext.StopTimingExecution();
			}

			return taskExecutor != null
				? CreateExecutionResult( executionContext )
				: null;
		}

		private IQueuedTask ExtractTask( TaskExecutionContext executionContext )
		{
			return executionContext
				.TaskToken
				.DequeuedTask;
		}

		private ITaskExecutor ResolveTaskExecutor( IQueuedTask queuedTask )
		{
			return mExecutorResolver
				.ResolveExecutor( queuedTask );
		}

		private void HandleTaskProcessingError( TaskExecutionContext executionContext,
			IQueuedTask dequeuedTask,
			Exception exc )
		{
			mLogger.Error( "Error executing queued task",
				exc );

			bool isRecoverable = mOptions
				.IsTaskErrorRecoverable( dequeuedTask, exc );

			executionContext?.SetTaskErrored( new QueuedTaskError( exc ),
				isRecoverable );
		}

		private TaskExecutionResult CreateExecutionResult( TaskExecutionContext executionContext )
		{
			DateTimeOffset retryAt = DateTimeOffset.UtcNow;

			//Compute the amount of time to delay task execution
			//	if execution failed
			if ( executionContext.HasResult
				&& executionContext.ExecutionFailed )
				retryAt = ComputeRetryAt( executionContext.TaskToken );

			return new TaskExecutionResult( executionContext.ResultInfo,
				executionContext.Duration,
				retryAt,
				mOptions.FaultErrorThresholdCount );
		}

		private DateTimeOffset ComputeRetryAt( IQueuedTaskToken queuedTaskToken )
		{
			return mRetryCalculator
				.ComputeRetryAt( queuedTaskToken );
		}
	}
}
