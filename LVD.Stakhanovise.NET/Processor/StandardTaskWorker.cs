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
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskWorker : ITaskWorker, IAppMetricsProvider
	{
		private readonly ITaskProcessor mTaskProcessor;

		private readonly ITaskExecutionResultProcessor mTaskResultProcessor;

		private readonly ITaskExecutionMetricsProvider mMetricsProvider;

		private readonly ITaskExecutorBufferHandlerFactory mBufferHandlerFactory;

		private readonly IStakhanoviseLogger mLogger;

		private CancellationTokenSource mStopCoordinator;

		private StateController mStateController = new StateController();

		private Task mWorkerTask;

		private ITaskExecutorBufferHandler mBufferHandler;

		private bool mIsDisposed = false;

		public StandardTaskWorker( ITaskProcessor taskProcessor,
			ITaskExecutionResultProcessor resultProcessor,
			ITaskExecutorBufferHandlerFactory bufferHandlerFactory,
			ITaskExecutionMetricsProvider metricsProvider,
			IStakhanoviseLogger logger )
		{
			mBufferHandlerFactory = bufferHandlerFactory
				?? throw new ArgumentNullException( nameof( bufferHandlerFactory ) );
			mTaskProcessor = taskProcessor
				?? throw new ArgumentNullException( nameof( taskProcessor ) );
			mTaskResultProcessor = resultProcessor
				?? throw new ArgumentNullException( nameof( resultProcessor ) );
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardTaskWorker ),
					"Cannot reuse a disposed task worker"
				);
			}
		}

		public Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			if ( !IsRunning )
				TryRequestStart();
			else
				mLogger.Debug( "Worker started or in the process of starting." );

			return Task.CompletedTask;
		}

		private void TryRequestStart()
		{
			mStateController.TryRequestStart( DoStartupSequence );
		}

		private void DoStartupSequence()
		{
			mLogger.Debug( "Worker is stopped. Starting..." );

			SetupEnvironment();
			BeginWorking();

			mLogger.Debug( "Worker successfully started." );
		}

		private void SetupEnvironment()
		{
			mStopCoordinator =
				new CancellationTokenSource();
			mBufferHandler = mBufferHandlerFactory
				.Create( mStopCoordinator.Token );
		}

		private void BeginWorking()
		{
			mWorkerTask = Task.Run( async ()
				=> await RunWorkerAsync( mStopCoordinator.Token ) );
		}

		private async Task RunWorkerAsync( CancellationToken stopToken )
		{
			while ( !stopToken.IsCancellationRequested )
			{
				try
				{
					await DoWorkerLoopAsync( stopToken );
				}
				catch ( OperationCanceledException )
				{
					mLogger.Debug( "Worker stop requested. Breaking loop..." );
					break;
				}
			}
		}

		private async Task DoWorkerLoopAsync( CancellationToken stopToken )
		{
			stopToken.ThrowIfCancellationRequested();
			mBufferHandler.WaitForTaskAvailability();

			stopToken.ThrowIfCancellationRequested();
			IQueuedTaskToken queuedTaskToken = mBufferHandler
				.TryGetNextTask();

			if ( queuedTaskToken != null )
				await ExecuteTaskAndHandleResultAsync( queuedTaskToken,
					stopToken );
			else
				mLogger.Debug( "Nothing to execute: no task was retrieved." );
		}

		private async Task ExecuteTaskAndHandleResultAsync( IQueuedTaskToken queuedTaskToken,
			CancellationToken stopToken )
		{
			mLogger.DebugFormat( "New task to execute retrieved from buffer: task id = {0}.",
				queuedTaskToken.DequeuedTask.Id );

			TaskExecutionContext executionContext =
				CreateExecutionContext( queuedTaskToken,
					stopToken );

			TaskProcessingResult processingResult =
				await ProcessTaskAsync( executionContext );

			//We will not observe cancellation token 
			//	during result processing:
			//	if task executed till the end, we must at least 
			//	attempt to set the result
			if ( processingResult != null )
			{
				UpdateTaskProcessingStats( processingResult );
				await ProcessResultAsync( processingResult );
			}

			mLogger.DebugFormat( "Done executing task with id = {0}.",
				queuedTaskToken.DequeuedTask.Id );
		}

		private TaskExecutionContext CreateExecutionContext( IQueuedTaskToken queuedTaskToken,
			CancellationToken stopToken )
		{
			return new TaskExecutionContext(
				queuedTaskToken,
				stopToken
			);
		}

		private async Task<TaskProcessingResult> ProcessTaskAsync( TaskExecutionContext executionContext )
		{
			return await mTaskProcessor.ProcessTaskAsync(
				executionContext
			);
		}

		private async Task ProcessResultAsync( TaskProcessingResult processingResult )
		{
			await mTaskResultProcessor.ProcessResultAsync( processingResult );
		}

		private void UpdateTaskProcessingStats( TaskProcessingResult processingResult )
		{
			mMetricsProvider.UpdateTaskProcessingStats( processingResult );
		}

		public async Task StopAync()
		{
			CheckNotDisposedOrThrow();
			if ( IsRunning )
				await TryRequestStopAsync();
			else
				mLogger.Debug( "Worker stopped or in the process of stopping." );
		}

		private async Task TryRequestStopAsync()
		{
			await mStateController.TryRequestStopAsync( DoShutdownSequenceAsync );
		}

		private async Task DoShutdownSequenceAsync()
		{
			mLogger.Debug( "Worker is started. Stopping..." );

			await StopWorkingAsync();
			CleanupEnvironment();

			mLogger.Debug( "Worker successfully stopped." );
		}

		private async Task StopWorkingAsync()
		{
			mStopCoordinator.Cancel();
			await mWorkerTask;
		}

		private void CleanupEnvironment()
		{
			if ( mBufferHandler is IDisposable )
				( ( IDisposable ) mBufferHandler ).Dispose();
			mBufferHandler = null;

			mStopCoordinator.Dispose();
			mStopCoordinator = null;

			mWorkerTask = null;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					mStateController = null;
				}

				mIsDisposed = true;
			}
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetricsProvider
				.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetricsProvider
				.CollectMetrics();
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		public IEnumerable<IAppMetricId> ExportedMetrics
		{
			get
			{
				return mMetricsProvider.ExportedMetrics;
			}
		}
	}
}
