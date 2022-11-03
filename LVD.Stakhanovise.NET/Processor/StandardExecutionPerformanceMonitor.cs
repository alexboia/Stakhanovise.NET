﻿// 
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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardExecutionPerformanceMonitor : IExecutionPerformanceMonitor,
		IAppMetricsProvider,
		IDisposable
	{
		private const int ProcessingBatchSize = 5;

		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private long mLastRequestId;

		private AsyncProcessingRequestBatchProcessor<ExecutionPerformanceMonitorWriteRequest> mBatchProcessor;

		private IExecutionPerformanceMonitorWriter mStatsWriter;

		private bool mIsDisposed = false;

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.PerfMonReportPostCount, 0 ),
			new AppMetric( AppMetricId.PerfMonReportWriteCount, 0 ),
			new AppMetric( AppMetricId.PerfMonMinimumReportWriteDuration, long.MaxValue ),
			new AppMetric( AppMetricId.PerfMonMaximumReportWriteDuration, long.MinValue ),
			new AppMetric( AppMetricId.PerfMonReportWriteRequestsTimeoutCount, 0 )
		);

		private string mProcessId;

		public StandardExecutionPerformanceMonitor( string processId )
		{
			if ( string.IsNullOrWhiteSpace( processId ) )
				throw new ArgumentNullException( nameof( processId ) );

			mProcessId = processId;
			mBatchProcessor = new AsyncProcessingRequestBatchProcessor<ExecutionPerformanceMonitorWriteRequest>( ProcessRequestBatchAsync, mLogger );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardExecutionPerformanceMonitor ),
					"Cannot reuse a disposed execution performance monitor"
				);
			}
		}

		private void CheckRunningOrThrow()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The execution performance monitor is not running." );
		}

		private void IncrementPerfMonPostCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PerfMonReportPostCount,
				m => m.Increment() );
		}

		private void IncrementPerfMonWriteCount( TimeSpan duration )
		{
			long durationMilliseconds = ( long ) Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.PerfMonReportWriteCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.PerfMonMinimumReportWriteDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.PerfMonMaximumReportWriteDuration,
				m => m.Max( durationMilliseconds ) );
		}

		private void IncrementPerfMonWriteRequestTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PerfMonReportWriteRequestsTimeoutCount,
				m => m.Increment() );
		}

		public async Task ReportExecutionTimeAsync( string payloadType, long durationMilliseconds, int timeoutMilliseconds )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			mLogger.DebugFormat( "Execution time {0} reported for payload {1}",
				durationMilliseconds,
				payloadType );

			ExecutionPerformanceMonitorWriteRequest processRequest =
				new ExecutionPerformanceMonitorWriteRequest( GenerateRequestId(),
					payloadType,
					durationMilliseconds,
					timeoutMilliseconds: timeoutMilliseconds,
					maxFailCount: 3 );

			await mBatchProcessor.PostRequestAsync( processRequest );
			IncrementPerfMonPostCount();
		}

		private long GenerateRequestId()
		{
			return Interlocked.Increment( ref mLastRequestId );
		}

		public async Task StartFlushingAsync( IExecutionPerformanceMonitorWriter writer )
		{
			CheckNotDisposedOrThrow();

			if ( !mBatchProcessor.IsRunning )
				await DoStartFlushingAsync( writer );
			else
				mLogger.Debug( "Flushing is already started. Nothing to be done." );
		}

		private async Task DoStartFlushingAsync( IExecutionPerformanceMonitorWriter writer )
		{
			mStatsWriter = writer;
			await mBatchProcessor.StartAsync();
		}

		private async Task ProcessRequestBatchAsync( AsyncProcessingRequestBatch<ExecutionPerformanceMonitorWriteRequest> currentBatch )
		{
			MonotonicTimestamp startWrite = MonotonicTimestamp
				.Now();

			try
			{
				List<TaskPerformanceStats> executionTimeInfoBatch =
					new List<TaskPerformanceStats>();

				foreach ( ExecutionPerformanceMonitorWriteRequest rq in currentBatch )
					executionTimeInfoBatch.Add( new TaskPerformanceStats( rq.PayloadType, rq.DurationMilliseconds ) );

				await mStatsWriter.WriteAsync( mProcessId, executionTimeInfoBatch );

				foreach ( ExecutionPerformanceMonitorWriteRequest rq in currentBatch )
					rq.SetCompleted( 1 );

				IncrementPerfMonWriteCount( MonotonicTimestamp
					.Since( startWrite ) );
			}
			catch ( Exception exc )
			{
				foreach ( ExecutionPerformanceMonitorWriteRequest rq in currentBatch )
				{
					rq.SetFailed( exc );
					if ( rq.CanBeRetried )
						await mBatchProcessor.PostRequestAsync( rq );
				}

				mLogger.Error( "Error processing performance stats batch", exc );
			}
		}

		public async Task StopFlushingAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mBatchProcessor.IsRunning )
				await DoStopFlushingAsync();
			else
				mLogger.Debug( "Flushing is already stopped. Nothing to be done." );
		}

		private async Task DoStopFlushingAsync()
		{
			await mBatchProcessor.StopAsync();
			mStatsWriter = null;
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
					StopFlushingAsync().Wait();
					mBatchProcessor.Dispose();
				}

				mIsDisposed = true;
			}
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mBatchProcessor.IsRunning;
			}
		}

		public IEnumerable<IAppMetricId> ExportedMetrics
		{
			get
			{
				return mMetrics.ExportedMetrics;
			}
		}
	}
}
