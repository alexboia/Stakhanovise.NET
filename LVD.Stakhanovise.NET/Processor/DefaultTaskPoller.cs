using log4net;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultTaskPoller : ITaskPoller
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private ITaskBuffer mTaskBuffer;

		private ITaskQueueConsumer mTaskQueueConsumer;

		private bool mIsDisposed = false;

		private StateController mStateController
			= new StateController();

		private ManualResetEvent mWaitForClearToDequeue
			= new ManualResetEvent( false );

		private string[] mRequiredPayloadTypes;

		private Task mPollTask;

		public DefaultTaskPoller ( ITaskQueueConsumer taskQueueConsumer,
			ITaskBuffer taskBuffer )
		{
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskPoller ), "Cannot reuse a disposed task poller" );
		}

		private void HandleQueuedTaskRetrievedFromBuffer ( object sender, EventArgs e )
		{
			mLogger.Debug( "Received new buffer space available notification. Will resume polling..." );
			mWaitForClearToDequeue.Set();
		}

		private void HandlePollForDequeueRequired ( object sender, ClearForDequeueEventArgs e )
		{
			mLogger.DebugFormat( "Received poll for dequeue required notification from queue. Reason = {0}. Will resume polling...", e.Reason );
			mWaitForClearToDequeue.Set();
		}

		private async Task PollForQueuedTasksAsync ()
		{
			while ( !mStateController.IsStopRequested )
			{
				//If the buffer is full, we wait for some space to become available,
				//  since, even if we can dequeue an task, 
				//  we won't have anywhere to place it yet and we 
				//  may be needlessly helding a lock to that task 
				if ( mTaskBuffer.IsFull )
				{
					mLogger.Debug( "Task buffer is full. Waiting for available space..." );
					await mWaitForClearToDequeue.ToTask();
				}

				//See that we have not been notified to proceed 
				//  as part of the stop operation
				if ( mStateController.IsStopRequested )
				{
					mLogger.Debug( "Stop requested. Breaking polling loop..." );
					break;
				}
				else
					mWaitForClearToDequeue.Reset();


				//If there is no task available in the queue, begin waiting for 
				//  a notification of new added tasks
				QueuedTask queuedTask = await mTaskQueueConsumer.DequeueAsync( mRequiredPayloadTypes );
				if ( queuedTask == null )
				{
					mLogger.Debug( "No task dequeued when polled. Waiting for available task..." );
					await mWaitForClearToDequeue.ToTask();
				}
				else
				{
					mLogger.DebugFormat( "Task found with id = {0}. Adding to task buffer...", queuedTask.Id );
					mTaskBuffer.TryAddNewTask( queuedTask );
				}

				//See that we have not been notified to proceed 
				//  as part of the stop operation
				if ( !mStateController.IsStopRequested )
					mWaitForClearToDequeue.Reset();
				else
					break;
			}

			mTaskBuffer.CompleteAdding();
		}

		public async Task StartAsync ( params string[] requiredPayloadTypes )
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStopped )
			{
				mLogger.Debug( "Task poller is stopped. Starting..." );
				await mStateController.TryRequestStartAsync( async () =>
				{
					//Set everything to proper initial state
					ResetState();

					//Save payload types
					mRequiredPayloadTypes = requiredPayloadTypes
						 ?? new string[ 0 ];

					//Register event handlers
					mTaskQueueConsumer.ClearForDequeue += HandlePollForDequeueRequired;
					mTaskBuffer.QueuedTaskRetrieved += HandleQueuedTaskRetrievedFromBuffer;

					if ( !mTaskQueueConsumer.IsReceivingNewTaskUpdates )
						await mTaskQueueConsumer.StartReceivingNewTaskUpdatesAsync();

					//Run the polling thread
					mPollTask = Task.Run( PollForQueuedTasksAsync );
				} );
			}
			else
				mLogger.Debug( "Task poller is already started. Nothing to be done." );
		}

		public async Task StopAync ()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStarted )
			{
				mLogger.Debug( "Task poller is started. Stopping..." );
				await mStateController.TryRequestStopASync( async () =>
				{
					//We may be waiting for the right conditions to
					//  try polling the queue and populating 
					//  the buffer respectively again
					//Thus, in order to avoid waiting for these conditions to be met 
					//  just to be able to stop we signal that processing can continue
					//  and the polling thread is responsible for double-checking that stopping 
					//  has not been requested in the mean-time
					mWaitForClearToDequeue.Set();
					await mPollTask;

					//Clean-up event handlers and reset state
					mTaskBuffer.QueuedTaskRetrieved -= HandleQueuedTaskRetrievedFromBuffer;
					mTaskQueueConsumer.ClearForDequeue -= HandlePollForDequeueRequired;

					if ( mTaskQueueConsumer.IsReceivingNewTaskUpdates )
						await mTaskQueueConsumer.StopReceivingNewTaskUpdatesAsync();

					//Set everything to proper final state
					ResetState();
				} );
			}
			else
				mLogger.Debug( "Task poller is already stopped. Nothing to be done." );
		}

		private void ResetState ()
		{
			mWaitForClearToDequeue.Reset();
			mPollTask = null;
		}

		private void DisposeWaitHandles ()
		{
			mWaitForClearToDequeue.Dispose();
			mWaitForClearToDequeue = null;
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					//Ensure we have stopped
					StopAync().Wait();

					//Clear wait handles
					DisposeWaitHandles();

					//It is not our responsibility to dispose 
					//  of the queue and the buffer
					//  since we are not the owner and we may 
					//  interfere with their orchestration
					mTaskBuffer = null;
					mTaskQueueConsumer = null;

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
