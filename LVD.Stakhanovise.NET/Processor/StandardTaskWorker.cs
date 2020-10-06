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
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskWorker : ITaskWorker
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
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

		private ITaskQueueTimingBelt mTimingBelt;

		private IExecutionPerformanceMonitor mExecutionPerformanceMonitor;

		private ITaskQueueProducer mTaskQueueProducer;

		private ITaskResultQueue mTaskResultQueue;

		private CancellationTokenSource mStopTokenSource;

		private Task mWorkerTask;

		private TaskProcessingOptions mOptions;

		public StandardTaskWorker (
			TaskProcessingOptions options,
			ITaskBuffer taskBuffer,
			ITaskExecutorRegistry executorRegistry,
			IExecutionPerformanceMonitor executionPerformanceMonitor,
			ITaskQueueProducer taskQueueProducer,
			ITaskResultQueue taskResultQueue,
			ITaskQueueTimingBelt timingBelt )
		{
			mOptions = options ??
				throw new ArgumentNullException( nameof( options ) );
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mExecutorRegistry = executorRegistry
				?? throw new ArgumentNullException( nameof( executorRegistry ) );
			mExecutionPerformanceMonitor = executionPerformanceMonitor
				?? throw new ArgumentNullException( nameof( executionPerformanceMonitor ) );
			mTimingBelt = timingBelt ??
				throw new ArgumentNullException( nameof( timingBelt ) );
			mTaskQueueProducer = taskQueueProducer ??
				throw new ArgumentNullException( nameof( taskQueueProducer ) );
			mTaskResultQueue = taskResultQueue ??
				throw new ArgumentNullException( nameof( taskResultQueue ) );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardTaskWorker ),
					"Cannot reuse a disposed task worker" );
		}

		private void HandleQueuedTaskAdded ( object sender, EventArgs e )
		{
			mWaitForClearToFetchTask.Set();
		}

		private Type DetectTaskPayloadType ( IQueuedTask queuedTask )
		{
			return queuedTask.Payload != null
				? queuedTask.Payload.GetType()
				: mExecutorRegistry.ResolvePayloadType( queuedTask.Type );
		}

		private ITaskExecutor ResolveTaskExecutor ( IQueuedTask queuedTask )
		{
			Type payloadType;
			ITaskExecutor taskExecutor = null;

			if ( ( payloadType = DetectTaskPayloadType( queuedTask ) ) != null )
			{
				mLogger.DebugFormat( "Runtime payload type {0} found for task type {1}.",
					payloadType,
					queuedTask.Type );

				taskExecutor = mExecutorRegistry
					.ResolveExecutor( payloadType );

				if ( taskExecutor != null )
					mLogger.DebugFormat( "Executor {0} found for task type {1}.",
						taskExecutor.GetType().FullName,
						queuedTask.Type );
				else
					mLogger.WarnFormat( "Executor not found for task type {0}.",
						queuedTask.Type );

			}
			else
				mLogger.WarnFormat( "Runtime payload type not found for task type {0}.",
					queuedTask.Type );

			return taskExecutor;
		}

		private async Task<TimedExecutionResult<TaskExecutionResultInfo>> ExecuteQueuedTaskAsync ( IQueuedTaskToken queuedTaskToken,
			CancellationToken stopToken )
		{
			ITaskExecutor taskExecutor = null;
			ITaskExecutionContext executionContext = null;
			IQueuedTask dequeuedTask = queuedTaskToken.DequeuedTask;

			MonotonicTimestamp start =
				MonotonicTimestamp.Now();

			try
			{
				//Initialize execution context
				executionContext = new TaskExecutionContext( dequeuedTask,
					cancellationToken: stopToken );

				//Check for cancellation before we start execution
				executionContext.ThrowIfCancellationRequested();

				//Attempt to resolve and run task executor
				taskExecutor = ResolveTaskExecutor( dequeuedTask );
				if ( taskExecutor != null )
				{
					mLogger.DebugFormat( "Beginning task execution. Task id = {0}.",
						dequeuedTask.Id );

					//Execute task
					await taskExecutor.ExecuteAsync( dequeuedTask.Payload,
						executionContext );

					mLogger.DebugFormat( "Task execution completed. Task id = {0}.",
						dequeuedTask.Id );

					//Check for cancellation after we completed execution
					executionContext.ThrowIfCancellationRequested();

					//Ensure we have a result - since no exception was thrown 
					//	and no result explicitly set, assume success.
					if ( !executionContext.HasResult )
						executionContext.NotifyTaskCompleted();
				}
			}
			catch ( OperationCanceledException )
			{
				//User code has observed cancellation request 
				executionContext?.NotifyCancellationObserved();
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error executing queued task",
					exception: exc );

				bool isRecoverable = mOptions.IsTaskErrorRecoverable( dequeuedTask,
					exc );

				executionContext?.NotifyTaskErrored( new QueuedTaskError( exc ),
					isRecoverable: isRecoverable );
			}

			MonotonicTimestamp end =
				MonotonicTimestamp.Now();

			return new TimedExecutionResult<TaskExecutionResultInfo>( taskExecutor != null
					? executionContext.ResultInfo
					: null,
				duration: end - start );
		}

		private async Task SetTaskExecutionResultAsync ( IQueuedTaskToken queuedTaskToken,
			TimedExecutionResult<TaskExecutionResultInfo> resultInfo )
		{
			long retryAtTicks = 0;

			try
			{
				if ( !resultInfo.HasResult )
				{
					mLogger.Debug( "No result info returned. Task token will be discarded." );
					return;
				}

				if ( resultInfo.Result.ExecutionCancelled )
				{
					mLogger.Debug( "Task execution cancelled. Task token will be discarded." );
					return;
				}

				//Task execution did not end successfully, 
				//	so we need to see how much we should delay its next execution
				if ( !resultInfo.Result.ExecutedSuccessfully )
				{
					mLogger.Debug( "Will compute task execution delay." );

					//Compute the amount of ticks to delay task execution
					long delayTicks = mOptions.CalculateDelayTicksTaskAfterFailure( queuedTaskToken
						.LastQueuedTaskResult
						.ErrorCount );

					retryAtTicks = await ComputeRetryAt( delayTicks );

					mLogger.DebugFormat( "Computed retry at ticks = {0}",
						retryAtTicks );
				}

				//Then try and post task execution result. 
				//	If this fails, we have no other option 
				//	than to discard the token
				QueuedTaskInfo repostWithInfo = queuedTaskToken.UdpateFromExecutionResult( new TaskExecutionResult( resultInfo,
					retryAtTicks: retryAtTicks,
					//TODO: fault error threshold count -> Move to processing options
					faultErrorThresholdCount: 5 ) );

				//Update result
				mLogger.Debug( "Will post task execution result." );
				await mTaskResultQueue.PostResultAsync( queuedTaskToken.LastQueuedTaskResult );
				mLogger.Debug( "Successfully posted task execution result." );

				if ( repostWithInfo != null )
				{
					mLogger.Debug( "Will repost task for execution." );
					await mTaskQueueProducer.EnqueueAsync( repostWithInfo );
					mLogger.Debug( "Sucessfully reposted task for execution." );
				}
				else
					mLogger.Debug( "Will not repost task for execution." );

				//Finally, report execution time
				mExecutionPerformanceMonitor.ReportExecutionTime( queuedTaskToken.DequeuedTask.Type,
					resultInfo.DurationMilliseconds );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Failed to set queued task result. Task token will be discarded.",
					exc );
			}
		}

		private async Task<long> ComputeRetryAt ( long delayTicks )
		{
			long retryAtTicks;
			AbstractTimestamp lastKnownTime =
				mTimingBelt.LastTime;

			try
			{
				//Compute the absolute time, in ticks, 
				//	until the task execution is delayed.
				retryAtTicks = await mTimingBelt.ComputeAbsoluteTimeTicksAsync( delayTicks );
			}
			catch ( Exception exc )
			{
				//Attempt failed, we will fallback to the last known time 
				//	+ the amount to delay
				retryAtTicks = lastKnownTime.Ticks + delayTicks;
				mLogger.Error( "Failed to compute absolute delay. Using last known time as a basis.",
					exc );
			}

			return retryAtTicks;
		}

		private async Task ExecuteQueuedTaskAndSetResultAsync ( IQueuedTaskToken queuedTaskToken, 
			CancellationToken stopToken )
		{
			mLogger.DebugFormat( "New task to execute retrieved from buffer: task id = {0}.",
				queuedTaskToken.DequeuedTask.Id );

			TimedExecutionResult<TaskExecutionResultInfo> resultInfo = 
				await ExecuteQueuedTaskAsync( queuedTaskToken, 
					stopToken );

			//We will not observe cancellation token 
			//	during result processing:
			//	if task executed till the end, we must at least 
			//	attempt to set the result
			await SetTaskExecutionResultAsync( queuedTaskToken,
				resultInfo );

			mLogger.DebugFormat( "Done executing task with id = {0}.",
				queuedTaskToken.DequeuedTask.Id );
		}

		private async Task<bool> PerformBufferCheckAsync ()
		{
			//Buffer has tasks, all good
			if ( mTaskBuffer.HasTasks )
				return true;

			//No tasks found in buffer, dig deeper
			mLogger.Debug( "No tasks found in buffer. Checking if buffer is completed..." );

			//It may be that it ran out of tasks because 
			//  it was marked as completed and all 
			//  the remaining tasks were consumed
			//In this case, waiting would mean waiting for ever, 
			//  since a completed buffer will no longer have 
			//  any items added to it
			if ( mTaskBuffer.IsCompleted )
			{
				mLogger.Debug( "Buffer completed, will break worker processing loop." );
				return false;
			}

			//TODO: if there are no more buffer items AND the buffer is set as completed, 
			//	it may take some time until we notice (and via other channels)
			//Wait for tasks to become available
			await mWaitForClearToFetchTask.ToTask();
			return true;
		}

		private async Task RunWorkerAsync ( CancellationToken stopToken )
		{
			//Check for cancellation before we start 
			//	the processing loop
			if ( stopToken.IsCancellationRequested )
				return;

			while ( true )
			{
				try
				{
					//Check for cancellation at the beginning 
					//	of processing each loop
					stopToken.ThrowIfCancellationRequested();

					//Check if buffer can deliver new tasks to us.
					//	If not (and it's permanent), break worker processing loop.
					if ( !await PerformBufferCheckAsync() )
						break;

					//It may be that the wait handle was signaled 
					//  as part of the Stop operation,
					//  so we need to check for that as well.
					stopToken.ThrowIfCancellationRequested();

					//Finally, dequeue and execute the task
					//  and forward the result to the result queue
					IQueuedTaskToken queuedTaskToken = mTaskBuffer.TryGetNextTask();
					if ( queuedTaskToken != null )
						await ExecuteQueuedTaskAndSetResultAsync( queuedTaskToken, stopToken );
					else
						mLogger.Debug( "Nothing to execute: no task was retrieved from buffer." );

					//At the end of the loop, reset the handle
					mWaitForClearToFetchTask.Reset();
				}
				catch ( OperationCanceledException )
				{
					mLogger.Debug( "Task worker stop requested. Breaking processing loop..." );
					break;
				}
			}
		}

		private void DoStartupSequence ( string[] requiredPayloadTypes )
		{
			mLogger.Debug( "Worker is stopped. Starting..." );

			//Save payload types
			mRequiredPayloadTypes = requiredPayloadTypes ?? new string[ 0 ];

			//Set everything to proper initial state
			//   and register event handlers
			mTaskBuffer.QueuedTaskAdded += HandleQueuedTaskAdded;

			//Run worker thread
			mStopTokenSource = new CancellationTokenSource();
			mWorkerTask = Task.Run( async () => await RunWorkerAsync( mStopTokenSource.Token ) );

			mLogger.Debug( "Worker successfully started." );
		}

		public Task StartAsync ( params string[] requiredPayloadTypes )
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				mStateController.TryRequestStart( ()
					=> DoStartupSequence( requiredPayloadTypes ) );
			else
				mLogger.Debug( "Worker is already started or in the process of starting." );

			return Task.CompletedTask;
		}

		private async Task DoShutdownSequenceAsync ()
		{
			mLogger.Debug( "Worker is started. Stopping..." );

			//Request to stop processing loop
			mStopTokenSource.Cancel();

			//We may be waiting for the right conditions to
			//  try polling the buffer again
			//Thus, in order to avoid waiting for these conditions to be met 
			//  just to be able to stop we signal that processing can continue
			//  and the polling thread is responsible for double-checking that stopping 
			//  has not been requested in the mean-time
			mWaitForClearToFetchTask.Set();

			//Wait for the processing loop to be stopped
			await mWorkerTask;

			//Clean-up event handlers and reset state
			mTaskBuffer.QueuedTaskAdded -= HandleQueuedTaskAdded;

			mWaitForClearToFetchTask.Reset();
			mWorkerTask = null;

			mStopTokenSource.Dispose();
			mStopTokenSource = null;

			mLogger.Debug( "Worker successfully stopped." );
		}

		public async Task StopAync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async ()
					=> await DoShutdownSequenceAsync() );
			else
				mLogger.Debug( "Worker is already stopped or in the process of stopping." );
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
					mWaitForClearToFetchTask.Dispose();
					mWaitForClearToFetchTask = null;

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
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
