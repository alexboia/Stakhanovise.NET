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
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskEngine : ITaskEngine
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private ITaskQueueConsumer mTaskQueueConsumer;

		private ITaskBuffer mTaskBuffer;

		private ITaskPoller mTaskPoller;

		private ITaskExecutorRegistry mExecutorRegistry;

		private ITaskQueueTimingBelt mTimingBelt;

		private IExecutionPerformanceMonitor mExecutionPerfMon;

		private IExecutionPerformanceMonitorWriter mExecutionPerfMonWriter;

		private List<ITaskWorker> mWorkers = new List<ITaskWorker>();

		private StateController mStateController
			= new StateController();

		private bool mIsDisposed;

		private TaskEngineOptions mOptions;

		public StandardTaskEngine ( TaskEngineOptions engineOptions,
			TaskQueueConsumerOptions consumerOptions,
			ITaskExecutorRegistry executorRegistry,
			ITaskQueueTimingBelt timingBelt,
			IExecutionPerformanceMonitorWriter executionPerfMonWriter )
		{
			if ( engineOptions == null )
				throw new ArgumentNullException( nameof( engineOptions ) );

			if ( consumerOptions == null )
				throw new ArgumentNullException( nameof( consumerOptions ) );

			mExecutorRegistry = executorRegistry
				?? throw new ArgumentNullException( nameof( executorRegistry ) );

			mTimingBelt = timingBelt
				?? throw new ArgumentNullException( nameof( timingBelt ) );
			mExecutionPerfMonWriter = executionPerfMonWriter
				?? throw new ArgumentNullException( nameof( executionPerfMonWriter ) );

			mExecutionPerfMon = new StandardExecutionPerformanceMonitor();
			mTaskQueueConsumer = new PostgreSqlTaskQueueConsumer( consumerOptions );

			mTaskBuffer = new StandardTaskBuffer( engineOptions.WorkerCount );
			mTaskPoller = new StandardTaskPoller( engineOptions.TaskProcessingOptions,
					mTaskQueueConsumer,
					mTaskBuffer,
					mExecutionPerfMon,
					mTimingBelt );

			mOptions = engineOptions;
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardTaskEngine ),
					"Cannot reuse a disposed task result queue" );
		}

		private async Task DoStartupSequenceAsync ()
		{
			mLogger.DebugFormat( "Attempting to start the task engine with {0} workers",
				mOptions.WorkerCount );

			string[] requiredPayloadTypes =
				GetRequiredPayloadTypeNames();

			mLogger.DebugFormat( "Found payload types: {0}.",
				string.Join( ",", requiredPayloadTypes ) );

			//Start the task poller and then start workers
			await StartTimingBeltAsync();
			await StartPollerAsync( requiredPayloadTypes );
			await StartWorkersAsync( requiredPayloadTypes );
			await StartFlushingPerformanceStatsAsync();

			mLogger.Debug( "The task engine has been successfully started." );
		}

		public async Task StartAsync ()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await DoStartupSequenceAsync() );
			else
				mLogger.Info( "The task engine is already started." );
		}

		private async Task DoShutdownSequenceAsync ()
		{
			mLogger.Debug( "Attempting to stop the task engine." );

			//Stop the task poller and then stop the workers
			await StopPollerAsync();
			await StopWorkersAsync();
			await StopTimingBeltAsync();
			await StopFlushingPerformanceStatsAsync();

			mLogger.Debug( "The task engine has been successfully stopped." );
		}

		public async Task StopAync ()
		{
			CheckDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async ()
					=> await DoShutdownSequenceAsync() );
			else
				mLogger.Debug( "The task engine is already stopped." );
		}

		private string[] GetRequiredPayloadTypeNames ()
		{
			return mExecutorRegistry
				.DetectedPayloadTypes
				.Select( t => t.FullName )
				.ToArray() ?? new string[ 0 ];
		}

		public void ScanAssemblies ( params Assembly[] assemblies )
		{
			mLogger.Debug( "Scanning given assemblies for task executors..." );
			mExecutorRegistry.ScanAssemblies( assemblies );
			mLogger.Debug( "Done scanning given assemblies for task executors." );
		}

		private async Task StartFlushingPerformanceStatsAsync ()
		{
			if ( mOptions.PerfMonOptions.FlushStats )
				await mExecutionPerfMon.StartFlushingAsync( mExecutionPerfMonWriter, mOptions.PerfMonOptions.FlushOptions );
		}

		private async Task StopFlushingPerformanceStatsAsync ()
		{
			if ( mOptions.PerfMonOptions.FlushStats )
				await mExecutionPerfMon.StopFlushingAsync();
		}

		private async Task StartPollerAsync ( string[] requiredPayloadTypes )
		{
			mLogger.Debug( "Attempting to start the task poller..." );
			await mTaskPoller.StartAsync( requiredPayloadTypes );
			mLogger.Debug( "The task poller has been successfully started. Attempting to start workers." );
		}

		private async Task StopPollerAsync ()
		{
			mLogger.Debug( "Attempting to stop the task poller." );
			await mTaskPoller.StopAync();
			mLogger.Debug( "The task poller has been successfully stopped. Attempting to stop workers." );
		}

		private async Task StartTimingBeltAsync ()
		{
			mLogger.Debug( "Attempting to start the timing belt" );
			await mTimingBelt.StartAsync();
			mLogger.Debug( "Timing belt successfully started" );
		}

		private async Task StopTimingBeltAsync ()
		{
			mLogger.Debug( "Attempting to stop the timing belt" );
			await mTimingBelt.StopAsync();
			mLogger.Debug( "Timing belt successfully stopped" );
		}

		private async Task StartWorkersAsync ( string[] requiredPayloadTypes )
		{
			mLogger.Debug( "Attempting to start workers..." );

			for ( int i = 0; i < mOptions.WorkerCount; i++ )
			{
				ITaskWorker taskWorker = new StandardTaskWorker( mOptions.TaskProcessingOptions,
					mTaskBuffer,
					mExecutorRegistry,
					mExecutionPerfMon,
					mTimingBelt );

				await taskWorker.StartAsync( requiredPayloadTypes );
				mWorkers.Add( taskWorker );
			}

			mLogger.Debug( "All the workers have been successfully started." );
		}

		private async Task StopWorkersAsync ()
		{
			mLogger.Debug( "Attempting to stop workers..." );

			foreach ( ITaskWorker worker in mWorkers )
				await TryStopWorkerAsync( worker );
			mWorkers.Clear();

			mLogger.Debug( "All the workers have been successfully stopped." );
		}

		private async Task TryStopWorkerAsync ( ITaskWorker worker )
		{
			using ( worker )
				await worker.StopAync();
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAync().Wait();

					mTaskPoller.Dispose();
					mTaskBuffer.Dispose();

					mTaskPoller = null;
					mTaskBuffer = null;
					mTimingBelt = null;

					mTaskQueueConsumer = null;
					mExecutorRegistry = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
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
