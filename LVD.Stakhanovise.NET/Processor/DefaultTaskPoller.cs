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

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskPoller ),
					"Cannot reuse a disposed task poller" );
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
				QueuedTask queuedTask = await mTaskQueueConsumer
					.DequeueAsync( mRequiredPayloadTypes );

				if ( queuedTask != null )
				{
					mLogger.DebugFormat( "Task found with id = {0}. Adding to task buffer...", queuedTask.Id );
					mTaskBuffer.TryAddNewTask( queuedTask );
				}
				else
				{
					mLogger.Debug( "No task dequeued when polled. Waiting for available task..." );
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
			}

			mTaskBuffer.CompleteAdding();
		}

		public async Task StartAsync ( params string[] requiredPayloadTypes )
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
			{
				mLogger.Debug( "Task poller is stopped. Starting..." );
				await mStateController.TryRequestStartAsync( async () =>
				{
					//Save payload types
					mRequiredPayloadTypes = requiredPayloadTypes
						 ?? new string[ 0 ];

					//Register event handlers
					mTaskQueueConsumer.ClearForDequeue +=
						HandlePollForDequeueRequired;
					mTaskBuffer.QueuedTaskRetrieved +=
						HandleQueuedTaskRetrievedFromBuffer;

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
			CheckNotDisposedOrThrow();

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
					mTaskBuffer.QueuedTaskRetrieved -=
						HandleQueuedTaskRetrievedFromBuffer;
					mTaskQueueConsumer.ClearForDequeue -=
						HandlePollForDequeueRequired;

					if ( mTaskQueueConsumer.IsReceivingNewTaskUpdates )
						await mTaskQueueConsumer.StopReceivingNewTaskUpdatesAsync();

					//Set everything to proper final state
					mWaitForClearToDequeue.Reset();
					mPollTask = null;
				} );
			}
			else
				mLogger.Debug( "Task poller is already stopped. Nothing to be done." );
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
					mWaitForClearToDequeue.Dispose();
					mWaitForClearToDequeue = null;

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
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
