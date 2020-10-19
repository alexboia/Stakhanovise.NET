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

		private long mLastRequestId;

		private Task mStatsProcessingTask = null;

		private CancellationTokenSource mStatsProcessingStopTokenSource = null;

		private StateController mStateController =
			new StateController();

		private BlockingCollection<StandardExecutionPerformanceMonitorWriteRequest> mStatsProcessingQueue =
			new BlockingCollection<StandardExecutionPerformanceMonitorWriteRequest>();

		private IExecutionPerformanceMonitorWriter mStatsWriter;

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardExecutionPerformanceMonitor ),
					"Cannot reuse a disposed execution performance monitor" );
		}

		private void CheckRunningOrThrow ()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The execution performance monitor is not running." );
		}

		public Task<int> ReportExecutionTimeAsync ( string payloadType, long durationMilliseconds, int timeoutMilliseconds )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			mLogger.DebugFormat( "Execution time {0} reported for payload {1}",
				durationMilliseconds,
				payloadType );

			long requestId = Interlocked.Increment( ref mLastRequestId );

			TaskCompletionSource<int> completionToken =
				new TaskCompletionSource<int>( TaskCreationOptions
					.RunContinuationsAsynchronously );

			StandardExecutionPerformanceMonitorWriteRequest request =
				new StandardExecutionPerformanceMonitorWriteRequest( requestId, payloadType, durationMilliseconds,
					completionToken,
					timeoutMilliseconds: timeoutMilliseconds,
					maxFailCount: 3 );

			mStatsProcessingQueue.Add( request );

			return completionToken.Task.WithCleanup( ( prev )
				=> request.Dispose() );
		}

		private async Task ProcessStatsBatchAsync ( IEnumerable<StandardExecutionPerformanceMonitorWriteRequest> currentBatch )
		{
			List<TaskPerformanceStats> executionTimeInfoBatch =
				new List<TaskPerformanceStats>();

			try
			{
				foreach ( StandardExecutionPerformanceMonitorWriteRequest rq in currentBatch )
					executionTimeInfoBatch.Add( new TaskPerformanceStats( rq.PayloadType, rq.DurationMilliseconds ) );

				await mStatsWriter.WriteAsync( executionTimeInfoBatch );

				foreach ( StandardExecutionPerformanceMonitorWriteRequest rq in currentBatch )
					rq.SetCompleted( 1 );
			}
			catch ( Exception exc )
			{
				foreach ( StandardExecutionPerformanceMonitorWriteRequest rq in currentBatch )
				{
					rq.SetFailed( exc );
					if ( rq.CanBeRetried )
						mStatsProcessingQueue.Add( rq );
				}

				mLogger.Error( "Error processing performance stats batch", exc );
			}
		}

		private async Task RunFlushLoopAsync ()
		{
			CancellationToken stopToken = mStatsProcessingStopTokenSource
				.Token;

			if ( stopToken.IsCancellationRequested )
				return;

			while ( true )
			{
				List<StandardExecutionPerformanceMonitorWriteRequest> currentBatch =
					new List<StandardExecutionPerformanceMonitorWriteRequest>();

				try
				{
					stopToken.ThrowIfCancellationRequested();

					//Try to dequeue and block if no item is available
					StandardExecutionPerformanceMonitorWriteRequest processItem =
						 mStatsProcessingQueue.Take( stopToken );

					currentBatch.Add( processItem );

					//See if there are other items available 
					//	and add them to current batch
					while ( currentBatch.Count < 5 && mStatsProcessingQueue.TryTake( out processItem ) )
						currentBatch.Add( processItem );

					//Process the entire batch - don't observe 
					//	cancellation token
					await ProcessStatsBatchAsync( currentBatch );
				}
				catch ( OperationCanceledException )
				{
					//Best effort to cancel all tasks
					foreach ( StandardExecutionPerformanceMonitorWriteRequest rq in mStatsProcessingQueue.ToArray() )
						rq.SetCancelled();

					mLogger.Debug( "Cancellation requested. Breaking stats processing loop..." );
					break;
				}
				catch ( Exception exc )
				{
					//Add them back to processing queue to be retried
					foreach ( StandardExecutionPerformanceMonitorWriteRequest rq in currentBatch )
					{
						rq.SetFailed( exc );
						if ( rq.CanBeRetried )
							mStatsProcessingQueue.Add( rq );
					}

					mLogger.Error( "Error processing results", exc );
				}
				finally
				{
					//Clear batch and start over
					currentBatch.Clear();
				}
			}
		}

		private async Task DoFlushingStartupSequenceAsync ( IExecutionPerformanceMonitorWriter writer )
		{
			mStatsWriter = writer;
			mStatsProcessingStopTokenSource =
				new CancellationTokenSource();

			await mStatsWriter.SetupIfNeededAsync();
			mStatsProcessingTask = Task.Run( async ()
				=> await RunFlushLoopAsync() );
		}

		public async Task StartFlushingAsync ( IExecutionPerformanceMonitorWriter writer )
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await DoFlushingStartupSequenceAsync( writer ) );
			else
				mLogger.Debug( "Flush scheduler is already started. Nothing to be done." );
		}

		private async Task DoFlushingShutdownSequenceAsync ()
		{
			mStatsProcessingQueue.CompleteAdding();
			mStatsProcessingStopTokenSource.Cancel();
			await mStatsProcessingTask;

			mStatsProcessingTask.Dispose();
			mStatsProcessingStopTokenSource.Dispose();
			mStatsProcessingQueue.Dispose();

			mStatsProcessingTask = null;
			mStatsProcessingQueue = null;
			mStatsProcessingStopTokenSource = null;
			mStatsWriter = null;
		}

		public async Task StopFlushingAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStartAsync( async ()
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
