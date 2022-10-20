// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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

		private ITaskQueueProducer mTaskQueueProducer;

		private string[] mRequiredPayloadTypes;

		private TaskProcessingOptions mOptions;

		private Task mQueuedTaskPollingWorker;

		private CancellationTokenSource mStopTokenSource;

		private ITaskPollerSynchronizationPolicy mSyncPolicy;

		private StateController mStateController
			= new StateController();

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.PollerDequeueCount,
			AppMetricId.PollerReturnedTaskCount
		);

		private bool mIsDisposed = false;

		public StandardTaskPoller( TaskProcessingOptions options,
			ITaskQueueConsumer taskQueueConsumer,
			ITaskQueueProducer taskQueueProducer,
			ITaskBuffer taskBuffer )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mQueuedTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
			mTaskQueueProducer = taskQueueProducer
				?? throw new ArgumentNullException( nameof( taskQueueProducer ) );

			mSyncPolicy = new ManualResetEventTaskPollerSynchronizationPolicy( mTaskQueueConsumer,
				mQueuedTaskBuffer );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardTaskPoller ),
					"Cannot reuse a disposed task poller"
				);
			}
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
			SetupTaskPollingSynchronization();
			await SetupTaskQueueConsumerAsync();
			StartTaskPolling();

			mLogger.Debug( "Successfully started task poller." );
		}

		private void UsePayloadTypes( string[] requiredPayloadTypes )
		{
			mRequiredPayloadTypes = requiredPayloadTypes
				?? new string[ 0 ];
		}

		private void SetupTaskPollingSynchronization()
		{
			mSyncPolicy.NotifyPollerStarted();
		}

		private async Task SetupTaskQueueConsumerAsync()
		{
			if ( !mTaskQueueConsumer.IsReceivingNewTaskUpdates )
				await mTaskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
		}

		private void StartTaskPolling()
		{
			mStopTokenSource = new CancellationTokenSource();
			mQueuedTaskPollingWorker = Task.Run( async ()
				=> await PollForQueuedTasksAsync() );
		}

		private async Task PollForQueuedTasksAsync()
		{
			CancellationToken stopToken = mStopTokenSource.Token;
			if ( stopToken.IsCancellationRequested )
				return;

			try
			{
				while ( true )
					await RunQueuedTaskPollingIterationAsync( stopToken );
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

		private async Task RunQueuedTaskPollingIterationAsync( CancellationToken stopToken )
		{
			stopToken.ThrowIfCancellationRequested();

			//If the buffer is full, we wait for some space to become available,
			//  since, even if we can dequeue an task, 
			//  we won't have anywhere to place it yet and we 
			//  may be needlessly helding a lock to that task 
			if ( IsTaskBufferFull() )
				WaitForClearToAddToBufer( stopToken );

			IQueuedTaskToken dequeuedTaskToken = await TryDequeueTaskAsync( stopToken );
			if ( dequeuedTaskToken != null )
				await ProcessDequeuedTaskAsync( dequeuedTaskToken, stopToken );
			else
				WaitForClearToDequeue( stopToken );
		}

		private bool IsTaskBufferFull()
		{
			return mQueuedTaskBuffer.IsFull;
		}

		private void WaitForClearToAddToBufer( CancellationToken stopToken )
		{
			mLogger.Debug( "Task buffer is full. Waiting for available space..." );
			mSyncPolicy.WaitForClearToAddToBuffer( stopToken );
		}

		private async Task<IQueuedTaskToken> TryDequeueTaskAsync( CancellationToken stopToken )
		{
			stopToken.ThrowIfCancellationRequested();

			IQueuedTaskToken dequeuedTaskToken = await mTaskQueueConsumer
				.DequeueAsync( mRequiredPayloadTypes );

			if ( dequeuedTaskToken != null )
				IncrementPollerDequeueCount();

			return dequeuedTaskToken;
		}

		private void IncrementPollerDequeueCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerDequeueCount,
				m => m.Increment() );
		}

		private async Task ProcessDequeuedTaskAsync( IQueuedTaskToken dequeuedTaskToken, CancellationToken stopToken )
		{
			mLogger.DebugFormat( "Task found with id = {0}, type = {1}.",
				dequeuedTaskToken.DequeuedTask.Id,
				dequeuedTaskToken.DequeuedTask.Type );

			bool returnToQueue = true;
			if ( !stopToken.IsCancellationRequested )
				returnToQueue = !TryAddDequeuedTaskToBuffer( dequeuedTaskToken );

			if ( returnToQueue )
				await ReturnDequeuedTaskToQueueAsync( dequeuedTaskToken );

			stopToken.ThrowIfCancellationRequested();
		}

		private bool TryAddDequeuedTaskToBuffer( IQueuedTaskToken queuedTaskToken )
		{
			return mQueuedTaskBuffer.TryAddNewTask( queuedTaskToken );
		}

		private async Task ReturnDequeuedTaskToQueueAsync( IQueuedTaskToken dequeuedTaskToken )
		{
			mLogger.DebugFormat( "Returning task id = {0}, type = {1} to queue.",
				dequeuedTaskToken.DequeuedTask.Id,
				dequeuedTaskToken.DequeuedTask.Type );

			await ProduceNewTaskFromDequeuedTaskToken( dequeuedTaskToken );
			IncrementPollerReturnedTaskCount();
		}

		private async Task ProduceNewTaskFromDequeuedTaskToken( IQueuedTaskToken dequeuedTaskToken )
		{
			QueuedTaskProduceInfo returnInfo = dequeuedTaskToken
				.GetReturnToQueueInfo();

			await mTaskQueueProducer
				.EnqueueAsync( returnInfo );
		}

		private void IncrementPollerReturnedTaskCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerReturnedTaskCount,
				m => m.Increment() );
		}

		private void WaitForClearToDequeue( CancellationToken stopToken )
		{
			mLogger.Debug( "No task dequeued when polled. Waiting for available task..." );
			mSyncPolicy.WaitForClearToDequeue( stopToken );
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
			await mStateController.TryRequestStopAsync( async ()
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
			mSyncPolicy.NotifyPollerStopRequested();
		}

		private void ResetTaskPollingSynchronization()
		{
			mSyncPolicy.Reset();
		}

		private async Task WaitForTaskPollingShutdownAsync()
		{
			await mQueuedTaskPollingWorker;
		}

		private async Task CleanupTaskQueueConsumerAsync()
		{
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
			IDisposable syncPolicyAsDisposable = mSyncPolicy as IDisposable;
			if ( syncPolicyAsDisposable != null )
				syncPolicyAsDisposable.Dispose();
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
			return AppMetricsCollection.JoinQueryMetric( metricId,
				mMetrics,
				mSyncPolicy );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return AppMetricsCollection.JoinCollectMetrics( mMetrics,
				mSyncPolicy );
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
				return AppMetricsCollection.JoinExportedMetrics( mMetrics,
					mSyncPolicy );
			}
		}
	}
}
