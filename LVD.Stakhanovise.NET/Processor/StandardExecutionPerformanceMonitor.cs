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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardExecutionPerformanceMonitor : IExecutionPerformanceMonitor, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private bool mIsDisposed = false;

		private Task mFlushTask = null;

		private CancellationTokenSource mFlushStopTokenSource = null;

		private CountdownEvent mFlushCountdownHandle = null;

		private StateController mFlushStateController =
			new StateController();

		private ConcurrentDictionary<string, TaskExecutionStats> mExecutionStats =
			new ConcurrentDictionary<string, TaskExecutionStats>();

		private IReadOnlyDictionary<string, TaskExecutionStats> mLastFlushedStats = null;

		private IExecutionPerformanceMonitorWriter mFlushWriter;

		private ExecutionPerformanceMonitorWriteOptions mFlushOptions;

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardExecutionPerformanceMonitor ),
					"Cannot reuse a disposed execution performance monitor" );
		}

		public TaskExecutionStats GetExecutionStats ( string payloadType )
		{
			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			if ( !mExecutionStats.TryGetValue( payloadType, out TaskExecutionStats executionTimeInfo ) )
				executionTimeInfo = TaskExecutionStats.Zero();

			return executionTimeInfo;
		}

		public void ReportExecutionTime ( string payloadType, long durationMilliseconds )
		{
			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			mLogger.DebugFormat( "Execution time {0} reported for payload {1}",
				durationMilliseconds,
				payloadType );

			mExecutionStats.AddOrUpdate( payloadType,
				addValueFactory: key => TaskExecutionStats.Initial( durationMilliseconds ),
				updateValueFactory: ( key, lastStats ) => lastStats.UpdateWithNewCycleExecutionTime( durationMilliseconds ) );

			if ( mFlushStateController.IsStarted )
				mFlushCountdownHandle.Signal();
		}

		private async Task RunFlushLoopAsync ( CancellationToken stopToken )
		{
			while ( true )
			{
				try
				{
					stopToken.ThrowIfCancellationRequested();

					if ( mFlushOptions.WriteIntervalThresholdMilliseconds > 0 )
						mFlushCountdownHandle.Wait( mFlushOptions.WriteIntervalThresholdMilliseconds, stopToken );
					else
						mFlushCountdownHandle.Wait( stopToken );

					stopToken.ThrowIfCancellationRequested();
					mFlushCountdownHandle.Reset();

					await mFlushWriter.WriteAsync( GetExecutionStatsChangesSinceLastFlush() );
					stopToken.ThrowIfCancellationRequested();
				}
				catch ( OperationCanceledException )
				{
					await mFlushWriter.WriteAsync( GetExecutionStatsChangesSinceLastFlush() );
					break;
				}
				catch ( Exception exc )
				{
					mLogger.Error( "Error flushing execution time info", exc );
				}
			}
		}

		private async Task DoFlushingStartupSequenceAsync ( IExecutionPerformanceMonitorWriter writer,
			ExecutionPerformanceMonitorWriteOptions options )
		{
			mFlushWriter = writer;
			mFlushOptions = options;
			mFlushStopTokenSource = new CancellationTokenSource();
			mFlushCountdownHandle = new CountdownEvent( options.WriteCountThreshold );

			await mFlushWriter.SetupIfNeededAsync();
			mFlushTask = Task.Run( async () => await RunFlushLoopAsync( mFlushStopTokenSource.Token ) );
		}

		public async Task StartFlushingAsync ( IExecutionPerformanceMonitorWriter writer,
			ExecutionPerformanceMonitorWriteOptions options )
		{
			CheckDisposedOrThrow();

			if ( mFlushStateController.IsStopped )
				await mFlushStateController.TryRequestStartAsync( async ()
					=> await DoFlushingStartupSequenceAsync( writer, options ) );
			else
				mLogger.Debug( "Flush scheduler is already started. Nothing to be done." );
		}

		private async Task DoFlushingShutdownSequenceAsync ()
		{
			mFlushStopTokenSource.Cancel();
			await mFlushTask;

			mFlushTask.Dispose();
			mFlushStopTokenSource.Dispose();
			mFlushCountdownHandle.Dispose();

			mFlushTask = null;
			mFlushStopTokenSource = null;
			mFlushCountdownHandle = null;
			mFlushOptions = null;
			mFlushWriter = null;
			mLastFlushedStats = null;
		}

		public async Task StopFlushingAsync ()
		{
			CheckDisposedOrThrow();

			if ( mFlushStateController.IsStarted )
				await mFlushStateController.TryRequestStartAsync( async ()
					=> await DoFlushingShutdownSequenceAsync() );
			else
				mLogger.Debug( "Flush scheduler is already stopped. Nothing to be done." );
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopFlushingAsync().Wait();

					mExecutionStats.Clear();
					mExecutionStats = null;
					mFlushStateController = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		private IReadOnlyDictionary<string, TaskExecutionStats> GetExecutionStatsChangesSinceLastFlush ()
		{
			Dictionary<string, TaskExecutionStats> statsDelta =
				new Dictionary<string, TaskExecutionStats>();

			IReadOnlyDictionary<string, TaskExecutionStats> currentStats =
				GetExecutionStatsSnpashot();

			IReadOnlyDictionary<string, TaskExecutionStats> prevStats = mLastFlushedStats == null
				? new ReadOnlyDictionary<string, TaskExecutionStats>( new Dictionary<string, TaskExecutionStats>() )
				: mLastFlushedStats;


			foreach ( KeyValuePair<string, TaskExecutionStats> tsPair in currentStats )
			{
				if ( !prevStats.TryGetValue( tsPair.Key, out TaskExecutionStats prevTs ) )
					prevTs = null;

				statsDelta.Add( tsPair.Key, tsPair.Value.Since( prevTs ) );
			}

			mLastFlushedStats = currentStats;
			return statsDelta;
		}

		private IReadOnlyDictionary<string, TaskExecutionStats> GetExecutionStatsSnpashot ()
		{
			Dictionary<string, TaskExecutionStats> snapshot =
				new Dictionary<string, TaskExecutionStats>();

			foreach ( KeyValuePair<string, TaskExecutionStats> tsInfo in mExecutionStats )
				snapshot.Add( tsInfo.Key, tsInfo.Value.Copy() );

			return new ReadOnlyDictionary<string, TaskExecutionStats>( snapshot );
		}

		public IReadOnlyDictionary<string, TaskExecutionStats> ExecutionStats =>
			GetExecutionStatsSnpashot();
	}
}
