// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskPoller : ITaskPoller, IAppMetricsProvider
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private ITaskBuffer mQueuedTaskBuffer;

		private ITaskQueueConsumer mTaskQueueConsumer;

		private string[] mRequiredPayloadTypes;

		private CancellationTokenSource mStopTokenSource;

		private Task mQueuedTaskPollingWorker;

		private TaskProcessingOptions mOptions;

		private StateController mStateController
			= new StateController();

		private ManualResetEvent mWaitForClearToDequeue
			= new ManualResetEvent( initialState: false );

		private ManualResetEvent mWaitForClearToAddToBuffer
			= new ManualResetEvent( initialState: false );

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.PollerDequeueCount,
			AppMetricId.PollerWaitForBufferSpaceCount,
			AppMetricId.PollerWaitForDequeueCount
		);

		private bool mIsDisposed = false;

		public StandardTaskPoller( TaskProcessingOptions options,
			ITaskQueueConsumer taskQueueConsumer,
			ITaskBuffer taskBuffer )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mQueuedTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardTaskPoller ),
					"Cannot reuse a disposed task poller" );
		}

		private void HandleQueuedTaskRetrievedFromBuffer( object sender, EventArgs e )
		{
			mLogger.Debug( "Received new buffer space available notification. Will resume polling..." );
			mWaitForClearToAddToBuffer.Set();
		}

		private void HandlePollForDequeueRequired( object sender, ClearForDequeueEventArgs e )
		{
			mLogger.DebugFormat( "Received poll for dequeue required notification from queue. Reason = {0}. Will resume polling...", e.Reason );
			mWaitForClearToDequeue.Set();
		}

		private async Task PollForQueuedTasksAsync()
		{
			CancellationToken stopToken = mStopTokenSource.Token;
			if ( stopToken.IsCancellationRequested )
				return;

			try
			{
				await RunQueuedTaskPollingLoopAsync( stopToken );
			}
			catch ( OperationCanceledException )
			{
				mLogger.Debug( "Stop requested. Polling loop has ended." );
			}
			finally
			{
				mQueuedTaskBuffer.CompleteAdding();
			}
		}

		private async Task RunQueuedTaskPollingLoopAsync( CancellationToken stopToken )
		{
			while ( true )
				await RunQueuedTaskPollingIterationAsync( stopToken );
		}

		private async Task RunQueuedTaskPollingIterationAsync( CancellationToken stopToken )
		{
			//Check for token cancellation at the beginning of the loop
			stopToken.ThrowIfCancellationRequested();

			//If the buffer is full, we wait for some space to become available,
			//  since, even if we can dequeue an task, 
			//  we won't have anywhere to place it yet and we 
			//  may be needlessly helding a lock to that task 
			if ( IsTaskBufferFull() )
				await WaitForTaskBufferAvailableSpaceAsync();

			//It may be that the wait handle was signaled 
			//  as part of the Stop operation,
			//  so we need to check for that as well.
			stopToken.ThrowIfCancellationRequested();

			IQueuedTaskToken queuedTaskToken =
				await TryDequeueTaskAsync();

			//Before posting the token to the buffer, 
			//	check if cancellation was requested
			stopToken.ThrowIfCancellationRequested();

			if ( queuedTaskToken != null )
				AddDequeuedTaskToBuffer( queuedTaskToken );
			else
				await WaitForAvailableTaskAsync();

			//It may be that the wait handle was signaled 
			//  as part of the Stop operation,
			//  so we need to check for that as well.
			stopToken.ThrowIfCancellationRequested();
			ResetTaskPollingSynchronization();
		}

		private bool IsTaskBufferFull()
		{
			return mQueuedTaskBuffer.IsFull;
		}

		private async Task WaitForTaskBufferAvailableSpaceAsync()
		{
			mLogger.Debug( "Task buffer is full. Waiting for available space..." );

			mMetrics.UpdateMetric( AppMetricId.PollerWaitForBufferSpaceCount,
				m => m.Increment() );

			await mWaitForClearToAddToBuffer.ToTask();
		}

		private async Task<IQueuedTaskToken> TryDequeueTaskAsync()
		{
			return await mTaskQueueConsumer.DequeueAsync( mRequiredPayloadTypes );
		}

		private void AddDequeuedTaskToBuffer( IQueuedTaskToken queuedTaskToken )
		{
			//If we have found a token, attempt to set it as started
			//	 and only then add it to buffer for processing.
			//If not, dispose and discard the token
			mLogger.DebugFormat( "Task found with id = {0}, type = {1}.",
				queuedTaskToken.DequeuedTask.Id,
				queuedTaskToken.DequeuedTask.Type );

			mMetrics.UpdateMetric( AppMetricId.PollerDequeueCount,
				m => m.Increment() );

			mQueuedTaskBuffer.TryAddNewTask( queuedTaskToken );
		}

		private async Task WaitForAvailableTaskAsync()
		{
			//If there is no task available in the queue, begin waiting for 
			//  a notification of new added tasks
			mLogger.Debug( "No task dequeued when polled. Waiting for available task..." );

			mMetrics.UpdateMetric( AppMetricId.PollerWaitForDequeueCount,
				m => m.Increment() );

			await mWaitForClearToDequeue
				.ToTask();
		}

		public async Task StartAsync( params string[] requiredPayloadTypes )
		{
			CheckNotDisposedOrThrow();

			if ( IsStopped )
				await TryRequestStartAsync( requiredPayloadTypes );
			else
				mLogger.Debug( "Task poller is already started. Nothing to be done." );
		}

		private async Task TryRequestStartAsync( string[] requiredPayloadTypes )
		{
			await mStateController.TryRequestStartAsync( async ()
				=> await DoStartupSequenceAsync( requiredPayloadTypes ) );
		}

		private async Task DoStartupSequenceAsync( string[] requiredPayloadTypes )
		{
			mLogger.Debug( "Task poller is stopped. Starting..." );

			UsePayloadTypes( requiredPayloadTypes );
			await SetupTaskQueueConsumerAsync();

			ResetTaskPollingSynchronization();
			StartTaskPolling();

			mLogger.Debug( "Successfully started task poller." );
		}

		private void UsePayloadTypes( string[] requiredPayloadTypes )
		{
			mRequiredPayloadTypes = requiredPayloadTypes
				?? new string[ 0 ];
		}

		private void ResetTaskPollingSynchronization()
		{
			mWaitForClearToDequeue.Reset();
			mWaitForClearToAddToBuffer.Reset();
		}

		private async Task SetupTaskQueueConsumerAsync()
		{
			mTaskQueueConsumer.ClearForDequeue +=
				HandlePollForDequeueRequired;
			mQueuedTaskBuffer.QueuedTaskRetrieved +=
				HandleQueuedTaskRetrievedFromBuffer;

			if ( !mTaskQueueConsumer.IsReceivingNewTaskUpdates )
				await mTaskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
		}

		private void StartTaskPolling()
		{
			mStopTokenSource = new CancellationTokenSource();
			mQueuedTaskPollingWorker = Task.Run( async () => await PollForQueuedTasksAsync() );
		}

		public async Task StopAync()
		{
			CheckNotDisposedOrThrow();

			if ( IsStarted )
				await TryRequestStopAsync();
			else
				mLogger.Debug( "Task poller is already stopped. Nothing to be done." );
		}

		private async Task TryRequestStopAsync()
		{
			await mStateController.TryRequestStopASync( async ()
				=> await DoShutdownSequenceAsync() );
		}

		private async Task DoShutdownSequenceAsync()
		{
			mLogger.Debug( "Task poller is started. Stopping..." );

			RequestTaskPollingCancellation();
			await WaitForTaskPollingShutdownAsync();

			await CleanupTaskQueueConsumerAsync();
			ResetTaskPollingSynchronization();

			CleanupTaskPolling();
			ResetPayloadTypes();

			mLogger.Debug( "Successfully stopped task poller." );
		}

		private void RequestTaskPollingCancellation()
		{
			mStopTokenSource.Cancel();

			//We may be waiting for the right conditions to
			//  try polling the queue (if the last poll yielded no task) 
			//	or for populating the buffer (if the buffer was full the last time we tried to post a task)
			//Thus, in order to avoid waiting for these conditions to be met 
			//  just to be able to stop we signal that processing can continue
			//  and the polling thread is responsible for double-checking that stopping 
			//  has not been requested in the mean-time
			mWaitForClearToDequeue.Set();
			mWaitForClearToAddToBuffer.Set();
		}

		private async Task WaitForTaskPollingShutdownAsync()
		{
			await mQueuedTaskPollingWorker;
		}

		private async Task CleanupTaskQueueConsumerAsync()
		{
			mQueuedTaskBuffer.QueuedTaskRetrieved -=
				HandleQueuedTaskRetrievedFromBuffer;
			mTaskQueueConsumer.ClearForDequeue -=
				HandlePollForDequeueRequired;

			if ( mTaskQueueConsumer.IsReceivingNewTaskUpdates )
				await mTaskQueueConsumer.StopReceivingNewTaskUpdatesAsync();
		}

		private void CleanupTaskPolling()
		{
			mQueuedTaskPollingWorker = null;
			mStopTokenSource.Dispose();
			mStopTokenSource = null;
		}

		private void ResetPayloadTypes()
		{
			mRequiredPayloadTypes = null;
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAync().Wait();
					CleanupTaskPollingSynchronization();
					ReleaseDependencies();
				}

				mIsDisposed = true;
			}
		}

		private void CleanupTaskPollingSynchronization()
		{
			mWaitForClearToDequeue.Dispose();
			mWaitForClearToDequeue = null;

			mWaitForClearToAddToBuffer.Dispose();
			mWaitForClearToAddToBuffer = null;
		}

		private void ReleaseDependencies()
		{
			mQueuedTaskBuffer = null;
			mTaskQueueConsumer = null;
			mStateController = null;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public AppMetric QueryMetric( AppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public bool IsStarted
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		private bool IsStopped
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStopped;
			}
		}

		public IEnumerable<AppMetricId> ExportedMetrics
		{
			get
			{
				return mMetrics.ExportedMetrics;
			}
		}
	}
}
