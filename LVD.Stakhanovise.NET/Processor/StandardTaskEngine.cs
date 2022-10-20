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
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskEngine : ITaskEngine, IAppMetricsProvider
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private PostgreSqlTaskQueueConsumer mTaskQueueConsumer;

		private StandardTaskBuffer mTaskBuffer;

		private StandardTaskPoller mTaskPoller;

		private ITaskExecutorRegistry mExecutorRegistry;

		private StandardExecutionPerformanceMonitor mExecutionPerfMon;

		private IExecutionPerformanceMonitorWriter mExecutionPerfMonWriter;

		private PostgreSqlTaskResultQueue mTaskResultQueue;

		private PostgreSqlTaskQueueProducer mTaskQueueProducer;

		private List<StandardTaskWorker> mWorkers =
			new List<StandardTaskWorker>();

		private StateController mStateController
			= new StateController();

		private bool mIsDisposed;

		private TaskEngineOptions mOptions;

		private AppMetricsCollection mMetricsSnapshot;

		public StandardTaskEngine( TaskEngineOptions engineOptions,
			TaskQueueOptions producerAndResultOptions,
			TaskQueueConsumerOptions consumerOptions,
			ITaskExecutorRegistry executorRegistry,
			IExecutionPerformanceMonitorWriter executionPerfMonWriter,
			ITimestampProvider timestampProvider,
			string processId )
		{
			if ( engineOptions == null )
				throw new ArgumentNullException( nameof( engineOptions ) );

			if ( consumerOptions == null )
				throw new ArgumentNullException( nameof( consumerOptions ) );

			if ( producerAndResultOptions == null )
				throw new ArgumentNullException( nameof( producerAndResultOptions ) );

			if ( string.IsNullOrWhiteSpace( processId ) )
				throw new ArgumentNullException( nameof( processId ) );

			mExecutorRegistry = executorRegistry
				?? throw new ArgumentNullException( nameof( executorRegistry ) );

			mExecutionPerfMonWriter = executionPerfMonWriter
				?? throw new ArgumentNullException( nameof( executionPerfMonWriter ) );

			mExecutionPerfMon = new StandardExecutionPerformanceMonitor( processId );
			mTaskQueueConsumer = new PostgreSqlTaskQueueConsumer( consumerOptions,
				timestampProvider );
			mTaskQueueProducer = new PostgreSqlTaskQueueProducer( producerAndResultOptions,
				timestampProvider );

			mTaskResultQueue = new PostgreSqlTaskResultQueue( producerAndResultOptions );

			mTaskBuffer = new StandardTaskBuffer( engineOptions.WorkerCount );
			mTaskPoller = new StandardTaskPoller( engineOptions.TaskProcessingOptions,
				mTaskQueueConsumer,
				mTaskQueueProducer,
				mTaskBuffer );

			mOptions = engineOptions;
		}

		private void CheckDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardTaskEngine ),
					"Cannot reuse a disposed task result queue"
				);
			}
		}

		private async Task DoStartupSequenceAsync()
		{
			mLogger.DebugFormat( "Attempting to start the task engine with {0} workers",
				mOptions.WorkerCount );

			string [] requiredPayloadTypes =
				GetRequiredPayloadTypeNames();

			mLogger.DebugFormat( "Found payload types: {0}.",
				string.Join( ",", requiredPayloadTypes ) );

			//Start the task poller and then start workers
			await StartResultQueueProcessingAsync();
			await StartExecutionPerformanceMonitorAsync();
			await StartPollerAsync( requiredPayloadTypes );
			await StartWorkersAsync();

			mLogger.Debug( "The task engine has been successfully started." );
		}

		public async Task StartAsync()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await DoStartupSequenceAsync() );
			else
				mLogger.Info( "The task engine is already started." );
		}

		private async Task DoShutdownSequenceAsync()
		{
			mLogger.Debug( "Attempting to stop the task engine." );

			//Stop the task poller and then stop the workers
			await StopPollerAsync();
			await StopWorkersAsync();
			await StopResultQueueProcessingAsync();
			await StopExecutionPerformanceMonitorAsync();

			StoreJoinedMetricsSnapshot();
			CleanupWorkers();

			mLogger.Debug( "The task engine has been successfully stopped." );
		}

		public async Task StopAync()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopAsync( async ()
					=> await DoShutdownSequenceAsync() );
			else
				mLogger.Debug( "The task engine is already stopped." );
		}

		private string [] GetRequiredPayloadTypeNames()
		{
			return mExecutorRegistry
				.DetectedPayloadTypeNames
				.ToArray();
		}

		public void ScanAssemblies( params Assembly [] assemblies )
		{
			mLogger.Debug( "Scanning given assemblies for task executors..." );
			mExecutorRegistry.ScanAssemblies( assemblies );
			mLogger.Debug( "Done scanning given assemblies for task executors." );
		}

		private async Task StartResultQueueProcessingAsync()
		{
			mLogger.Debug( "Starting result queue processing..." );
			await mTaskResultQueue.StartAsync();
			mLogger.Debug( "Successfully started result queue processing." );
		}

		private async Task StopResultQueueProcessingAsync()
		{
			mLogger.Debug( "Stopping result queue processing..." );
			await mTaskResultQueue.StopAsync();
			mLogger.Debug( "Successfully stopped result queue processing." );
		}

		private async Task StartExecutionPerformanceMonitorAsync()
		{
			mLogger.Debug( "Starting execution performance monitor..." );
			await mExecutionPerfMon.StartFlushingAsync( mExecutionPerfMonWriter );
			mLogger.Debug( "Successfully started execution performance monitor." );
		}

		private async Task StopExecutionPerformanceMonitorAsync()
		{
			mLogger.Debug( "Stopping execution performance monitor..." );
			await mExecutionPerfMon.StopFlushingAsync();
			mLogger.Debug( "Successfully stopped execution performance monitor." );
		}

		private async Task StartPollerAsync( string [] requiredPayloadTypes )
		{
			mLogger.Debug( "Attempting to start the task poller..." );
			await mTaskPoller.StartAsync( requiredPayloadTypes );
			mLogger.Debug( "The task poller has been successfully started. Attempting to start workers." );
		}

		private async Task StopPollerAsync()
		{
			mLogger.Debug( "Attempting to stop the task poller." );
			await mTaskPoller.StopAync();
			mLogger.Debug( "The task poller has been successfully stopped. Attempting to stop workers." );
		}

		private async Task StartWorkersAsync()
		{
			mLogger.Debug( "Attempting to start workers..." );

			for ( int i = 0; i < mOptions.WorkerCount; i++ )
				await CreateAndStartWorkerAsync();

			mLogger.Debug( "All the workers have been successfully started." );
		}

		private async Task CreateAndStartWorkerAsync()
		{
			IStakhanoviseLoggingProvider loggingProvider = StakhanoviseLogManager
				.Provider;

			ITaskExecutionMetricsProvider metricsProvider =
				new StandardTaskExecutionMetricsProvider();

			ITaskExecutorBufferHandlerFactory bufferHandlerFactory =
				new StandardTaskExecutorBufferHandlerFactory( mTaskBuffer,
					metricsProvider,
					loggingProvider );

			ITaskExecutorResolver taskExecutorResolver =
				new StandardTaskExecutorResolver( mExecutorRegistry,
					loggingProvider.CreateLogger<StandardTaskExecutorResolver>() );

			ITaskExecutionRetryCalculator executionRetryCalculator =
				new StandardTaskExecutionRetryCalculator( mOptions.TaskProcessingOptions,
					loggingProvider.CreateLogger<StandardTaskExecutionRetryCalculator>() );

			ITaskProcessor taskProcessor =
				new StandardTaskProcessor( mOptions.TaskProcessingOptions,
					taskExecutorResolver,
					executionRetryCalculator,
					loggingProvider.CreateLogger<StandardTaskProcessor>() );

			ITaskExecutionResultProcessor resultProcessor =
				new StandardTaskExecutionResultProcessor( mTaskResultQueue,
					mTaskQueueProducer,
					mExecutionPerfMon,
					loggingProvider.CreateLogger<StandardTaskExecutionResultProcessor>() );

			StandardTaskWorker taskWorker =
				new StandardTaskWorker( taskProcessor,
					resultProcessor,
					bufferHandlerFactory,
					metricsProvider,
					loggingProvider.CreateLogger<StandardTaskWorker>() );

			await taskWorker.StartAsync();
			mWorkers.Add( taskWorker );
		}

		private async Task StopWorkersAsync()
		{
			mLogger.Debug( "Attempting to stop workers..." );

			foreach ( ITaskWorker worker in mWorkers )
				await worker.StopAync();

			mLogger.Debug( "All the workers have been successfully stopped." );
		}

		protected void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAync().Wait();
					Cleanup();
				}

				mIsDisposed = true;
			}
		}

		private void Cleanup()
		{
			mTaskPoller.Dispose();
			mTaskBuffer.Dispose();

			if ( mTaskResultQueue is IDisposable )
				( ( IDisposable ) mTaskResultQueue ).Dispose();

			CleanupWorkers();

			mTaskPoller = null;
			mTaskBuffer = null;
			mTaskResultQueue = null;

			mTaskQueueConsumer = null;
			mExecutorRegistry = null;
		}

		private void CleanupWorkers()
		{
			foreach ( ITaskWorker worker in mWorkers )
				worker.Dispose();
			mWorkers.Clear();
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public AppMetric QueryMetric( AppMetricId metricId )
		{
			StoreJoinedMetricsSnapshot();
			return mMetricsSnapshot.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			StoreJoinedMetricsSnapshot();
			return mMetricsSnapshot.CollectMetrics();
		}

		private void StoreJoinedMetricsSnapshot()
		{
			if ( ShouldStoreMetricsSnapshot() )
			{
				mMetricsSnapshot = AppMetricsCollection.JoinProviders( mTaskBuffer,
					mTaskPoller,
					mTaskQueueConsumer,
					mTaskResultQueue,
					mExecutionPerfMon,
					AppMetricsCollection.JoinProviders( mWorkers ) );
			}
		}

		private bool ShouldStoreMetricsSnapshot()
		{
			return mStateController != null
				&& ( mStateController.IsStarted
					|| mStateController.IsStopRequested );
		}

		public IEnumerable<ITaskWorker> Workers
		{
			get
			{
				CheckDisposedOrThrow();
				return mWorkers.AsReadOnly();
			}
		}

		public ITaskPoller TaskPoller
		{
			get
			{
				CheckDisposedOrThrow();
				return mTaskPoller;
			}
		}

		public TaskEngineOptions Options
		{
			get
			{
				CheckDisposedOrThrow();
				return mOptions;
			}
		}

		public bool IsStarted
		{
			get
			{
				CheckDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		public IEnumerable<AppMetricId> ExportedMetrics
		{
			get
			{
				StoreJoinedMetricsSnapshot();
				return mMetricsSnapshot.ExportedMetrics;
			}
		}
	}
}
