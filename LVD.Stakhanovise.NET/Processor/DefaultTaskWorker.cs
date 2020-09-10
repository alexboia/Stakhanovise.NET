using log4net;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultTaskWorker : ITaskWorker
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private bool mIsDisposed = false;

		private string[] mRequiredPayloadTypes;

		private StateController mStateController
			= new StateController();

		private ManualResetEvent mWaitForClearToFetchTask
			= new ManualResetEvent( false );

		private ITaskBuffer mTaskBuffer;

		private ITaskExecutorRegistry mExecutorRegistry;

		private ITaskResultQueue mResultQueue;

		private Task mWorkerTask;

		public DefaultTaskWorker ( ITaskBuffer taskBuffer,
			ITaskExecutorRegistry executorRegistry,
			ITaskResultQueue resultQueue )
		{
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mExecutorRegistry = executorRegistry
				?? throw new ArgumentNullException( nameof( executorRegistry ) );
			mResultQueue = resultQueue
				?? throw new ArgumentNullException( nameof( resultQueue ) );
		}

		private void HandleQueuedTaskAdded ( object sender, EventArgs e )
		{
			mWaitForClearToFetchTask.Set();
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskWorker ), "Cannot reuse a disposed task worker" );
		}

		private bool CanBeExecuted ( QueuedTask queuedTask )
		{
			return queuedTask.Status == QueuedTaskStatus.Error
				|| queuedTask.Status == QueuedTaskStatus.Unprocessed
				|| queuedTask.Status == QueuedTaskStatus.Faulted;
		}

		private ITaskExecutor ResolveTaskExecutor ( QueuedTask queuedTask )
		{
			Type payloadType;
			ITaskExecutor taskExecutor = null;

			payloadType = mExecutorRegistry
				.ResolvePayloadType( queuedTask.Type );

			if ( payloadType != null )
			{
				mLogger.InfoFormat( "Runtime payload type found for task type {0}.",
					queuedTask.Type );

				taskExecutor = mExecutorRegistry
					.ResolveExecutor( payloadType );

				if ( taskExecutor == null )
					mLogger.WarnFormat( "Executor not found for task type {0}.",
						queuedTask.Type );
			}
			else
				mLogger.WarnFormat( "Runtime payload type not found for task type {0}.",
					queuedTask.Type );

			return taskExecutor;
		}

		private async Task<TaskExecutionResult> ExecuteQueuedTaskAsync ( QueuedTask queuedTask )
		{
			ITaskExecutor taskExecutor;
			ITaskExecutionContext executionContext;

			if ( !CanBeExecuted( queuedTask ) )
				return null;

			//Initialize execution context
			executionContext = new TaskExecutionContext( queuedTask );

			try
			{
				taskExecutor = ResolveTaskExecutor( queuedTask );
				if ( taskExecutor != null )
				{
					mLogger.InfoFormat( "Executor {0} found for task type {1}.",
						taskExecutor.GetType().FullName,
						queuedTask.Type );

					mLogger.DebugFormat( "Beginning task execution. Task id = {0}.",
						queuedTask.Id );

					//Execute task
					await taskExecutor.ExecuteAsync( queuedTask.Payload,
						executionContext );

					mLogger.DebugFormat( "Task execution completed. Task id = {0}.",
						queuedTask.Id );

					//Ensure we have a result
					if ( !executionContext.HasResult )
						executionContext.NotifyTaskCompleted();
				}
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error executing queued task",
					exception: exc );

				executionContext.NotifyTaskErrored( new QueuedTaskError( exc ),
					isRecoverable: false );
			}

			return executionContext.Result;
		}

		private async Task TryExecuteQueuedTaskAsync ( QueuedTask queuedTask )
		{
			TaskExecutionResult executionResult
				= await ExecuteQueuedTaskAsync( queuedTask );

			await mResultQueue.EnqueueResultAsync( queuedTask,
				executionResult );
		}

		private async Task RunWorkerAsync ()
		{
			while ( !mStateController.IsStopRequested && !mTaskBuffer.IsCompleted )
			{
				if ( !mTaskBuffer.HasTasks )
				{
					mLogger.Debug( "No tasks found in buffer. Checking if buffer is completed..." );

					//It may be that it ran out of tasks because 
					//  it was marked as completed and all 
					//  the remaining tasks were consumed
					//In this case, waiting would mean waiting for ever, 
					//  since a completed buffer will no longer have 
					//  any items added to it
					if ( mTaskBuffer.IsCompleted )
					{
						mLogger.Debug( "Task worker found buffer completed, will break worker processing loop." );
						break;
					}
					else
						await mWaitForClearToFetchTask.ToTask();
				}
				else
					mLogger.Debug( "Buffer has tasks. Checking if stop was requested..." );

				//It may be that the wait handle was signaled 
				//  as part of the Stop operation,
				//  so we need to check for that as well.
				if ( mStateController.IsStopRequested )
				{
					mLogger.Debug( "Task worker stop requested. Breaking processing loop..." );
					break;
				}

				//Reset the wait handle for fetching tasks, 
				//  since we obviously have tasks to process
				mWaitForClearToFetchTask.Reset();

				//Finally, dequeue and execute the task
				//  and forward the result to the result queue
				QueuedTask queuedTask = mTaskBuffer.TryGetNextTask();
				if ( queuedTask != null )
				{
					mLogger.DebugFormat( "New task to execute retrieved from buffer: task id = {0}.",
						queuedTask.Id );

					await TryExecuteQueuedTaskAsync( queuedTask );

					mLogger.DebugFormat( "Done executing task with id = {0}.",
						queuedTask.Id );
				}
				else
					mLogger.Debug( "Nothing to execute: no task was retrieved from buffer." );
			}
		}

		public async Task StartAsync ( params string[] requiredPayloadTypes )
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStopped )
			{
				await mStateController.TryRequestStartAsync( async () =>
				{
					//Set everything to proper initial state
					//   and register event handlers
					ResetState();
					mTaskBuffer.QueuedTaskAdded += HandleQueuedTaskAdded;

					//Save payload types
					mRequiredPayloadTypes = requiredPayloadTypes
						?? new string[ 0 ];

					//Run worker thread
					mWorkerTask = Task.Run( RunWorkerAsync );

					//Just remove the compiler warning
					await Task.CompletedTask;
				} );
			}
		}

		public async Task StopAync ()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStarted )
			{
				await mStateController.TryRequestStopASync( async () =>
				{
					//We may be waiting for the right conditions to
					//  try polling the buffer again
					//Thus, in order to avoid waiting for these conditions to be met 
					//  just to be able to stop we signal that processing can continue
					//  and the polling thread is responsible for double-checking that stopping 
					//  has not been requested in the mean-time
					mWaitForClearToFetchTask.Set();
					await mWorkerTask;

					//Clean-up event handlers and reset state
					mTaskBuffer.QueuedTaskAdded -= HandleQueuedTaskAdded;
					ResetState();
				} );
			}
		}

		private void ResetState ()
		{
			mWaitForClearToFetchTask.Reset();
			mWorkerTask = null;
		}

		private void DisposeWaitHandles ()
		{
			mWaitForClearToFetchTask.Dispose();
			mWaitForClearToFetchTask = null;
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					//Ensure we have stopped
					StopAync().Wait();

					//Clear wait handles
					DisposeWaitHandles();

					//It is not our responsibility to dispose of these dependencies
					//  since we are not the owner and we may interfere with their orchestration
					mTaskBuffer = null;
					mExecutorRegistry = null;

					mRequiredPayloadTypes = null;
					mStateController = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public bool IsRunning
		{
			get
			{
				CheckDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
