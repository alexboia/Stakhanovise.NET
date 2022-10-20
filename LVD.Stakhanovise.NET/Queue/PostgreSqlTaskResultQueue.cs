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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskResultQueue : ITaskResultQueue, IAppMetricsProvider, IDisposable
	{
		private const int RESULT_QUEUE_PROCESSING_BATCH_SIZE = 5;

		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private bool mIsDisposed = false;

		private TaskQueueOptions mOptions;

		private Task mResultProcessingTask;

		private CancellationTokenSource mResultProcessingStopRequest;

		private BlockingCollection<PostgreSqlTaskResultQueueProcessRequest> mResultProcessingQueue;

		private StateController mStateController =
			new StateController();

		private long mLastRequestId = 0;

		private string mUpdateSql;

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.ResultQueueMinimumResultWriteDuration, long.MaxValue ),
			new AppMetric( AppMetricId.ResultQueueMaximumResultWriteDuration, long.MinValue ),
			new AppMetric( AppMetricId.ResultQueueResultPostCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueResultWriteCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueResultWriteRequestTimeoutCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueTotalResultWriteDuration, 0 )
		);

		public PostgreSqlTaskResultQueue( TaskQueueOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );

			mUpdateSql = GetUpdateSql( options.Mapping );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskResultQueue ),
					"Cannot reuse a disposed postgre sql task result queue" );
		}

		private void CheckRunningOrThrow()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The task result queue is not running." );
		}

		private string GetUpdateSql( QueuedTaskMapping mapping )
		{
			return $@"UPDATE {mapping.ResultsQueueTableName} SET 
					task_status = @t_status,
					task_last_error = @t_last_error,
					task_error_count = @t_error_count,
					task_last_error_is_recoverable = @t_last_error_recoverable,
					task_processing_time_milliseconds = @t_processing_time_milliseconds,
					task_processing_finalized_at_ts = @t_processing_finalized_at_ts
				WHERE task_id = @t_id";
		}

		private void IncrementPostResultCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultPostCount,
				m => m.Increment() );
		}

		private void IncrementResultWriteCount( TimeSpan duration )
		{
			long durationMilliseconds = ( long ) Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultWriteCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueTotalResultWriteDuration,
				m => m.Add( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueMinimumResultWriteDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueMaximumResultWriteDuration,
				m => m.Max( durationMilliseconds ) );
		}

		private void IncrementResultWriteRequestTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultWriteRequestTimeoutCount,
				m => m.Increment() );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync( CancellationToken cancellationToken )
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync( cancellationToken );
		}

		private async Task PrepConnectionPoolAsync( CancellationToken cancellationToken )
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync( cancellationToken ) )
				await conn.CloseAsync();
		}

		public Task<int> PostResultAsync( IQueuedTaskToken token, int timeoutMilliseconds )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( token == null )
				throw new ArgumentNullException( nameof( token ) );

			long requestId = Interlocked.Increment( ref mLastRequestId );

			PostgreSqlTaskResultQueueProcessRequest processRequest =
				new PostgreSqlTaskResultQueueProcessRequest( requestId,
					token.LastQueuedTaskResult,
					timeoutMilliseconds: timeoutMilliseconds,
					maxFailCount: 3 );

			mResultProcessingQueue.Add( processRequest );
			IncrementPostResultCount();

			return processRequest.Task.WithCleanup( ( prev ) =>
			{
				if ( processRequest.IsTimedOut )
					IncrementResultWriteRequestTimeoutCount();
				processRequest.Dispose();
			} );
		}

		public Task<int> PostResultAsync( IQueuedTaskToken token )
		{
			return PostResultAsync( token,
				timeoutMilliseconds: 0 );
		}

		private async Task ProcessResultBatchAsync( Queue<PostgreSqlTaskResultQueueProcessRequest> currentBatch )
		{
			MonotonicTimestamp startWrite = MonotonicTimestamp
				.Now();

			//An explicit choice has been made not to use transactions
			//	since failing to update a result MUST NOT 
			//	cause the other successful updates to be rolled back.
			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
			using ( NpgsqlCommand updateCmd = new NpgsqlCommand( mUpdateSql, conn ) )
			{
				NpgsqlParameter pStatus = updateCmd.Parameters
					.Add( "t_status", NpgsqlDbType.Integer );
				NpgsqlParameter pLastError = updateCmd.Parameters
					.Add( "t_last_error", NpgsqlDbType.Text );
				NpgsqlParameter pErrorCount = updateCmd.Parameters
					.Add( "t_error_count", NpgsqlDbType.Integer );
				NpgsqlParameter pLastErrorIsRecoverable = updateCmd.Parameters
					.Add( "t_last_error_recoverable", NpgsqlDbType.Boolean );
				NpgsqlParameter pProcessingTime = updateCmd.Parameters
					.Add( "t_processing_time_milliseconds", NpgsqlDbType.Bigint );
				NpgsqlParameter pFinalizedAt = updateCmd.Parameters
					.Add( "t_processing_finalized_at_ts", NpgsqlDbType.TimestampTz );
				NpgsqlParameter pId = updateCmd.Parameters
					.Add( "t_id", NpgsqlDbType.Uuid );

				await updateCmd.PrepareAsync();

				while ( currentBatch.Count > 0 )
				{
					PostgreSqlTaskResultQueueProcessRequest processRq =
						currentBatch.Dequeue();

					try
					{
						pStatus.Value = ( int ) processRq
							.ResultToUpdate
							.Status;

						string strLastError = ConvertErrorInfoToJson( processRq
							.ResultToUpdate
							.LastError );

						if ( strLastError != null )
							pLastError.Value = strLastError;
						else
							pLastError.Value = DBNull.Value;

						pErrorCount.Value = processRq
							.ResultToUpdate
							.ErrorCount;

						pLastErrorIsRecoverable.Value = processRq
							.ResultToUpdate
							.LastErrorIsRecoverable;
						pProcessingTime.Value = processRq
							.ResultToUpdate
							.ProcessingTimeMilliseconds;

						if ( processRq.ResultToUpdate.ProcessingFinalizedAtTs.HasValue )
							pFinalizedAt.Value = processRq
								.ResultToUpdate
								.ProcessingFinalizedAtTs;
						else
							pFinalizedAt.Value = DBNull
								.Value;

						pId.Value = processRq
							.ResultToUpdate
							.Id;

						int affectedRows = await updateCmd.ExecuteNonQueryAsync();
						processRq.SetCompleted( affectedRows );

						IncrementResultWriteCount( MonotonicTimestamp
							.Since( startWrite ) );
					}
					catch ( OperationCanceledException )
					{
						processRq.SetCancelled();
						throw;
					}
					catch ( Exception exc )
					{
						processRq.SetFailed( exc );
						if ( processRq.CanBeRetried )
							mResultProcessingQueue.Add( processRq );

						mLogger.Error( "Error processing result", exc );
					}
				}

				await conn.CloseAsync();
			}
		}

		private string ConvertErrorInfoToJson( QueuedTaskError error )
		{
			return error.ToJson( mOptions
				.SerializerOptions
				.OnConfigureSerializerSettings );
		}

		private async Task RunProcessingLoopAsync()
		{
			CancellationToken stopToken = mResultProcessingStopRequest
				.Token;

			if ( stopToken.IsCancellationRequested )
				return;

			while ( true )
			{
				//We need to use a queue here - as we process the batch, 
				//	we consume each element and, in case of an error 
				//	that affects all of them, 
				//	we would fail only the remaining ones, not the ones 
				//	that have been successfully processed
				Queue<PostgreSqlTaskResultQueueProcessRequest> currentBatch =
					new Queue<PostgreSqlTaskResultQueueProcessRequest>();

				try
				{
					stopToken.ThrowIfCancellationRequested();

					//Try to dequeue and block if no item is available
					PostgreSqlTaskResultQueueProcessRequest processItem =
						 mResultProcessingQueue.Take( stopToken );

					currentBatch.Enqueue( processItem );

					//See if there are other items available 
					//	and add them to current batch
					while ( currentBatch.Count < RESULT_QUEUE_PROCESSING_BATCH_SIZE && mResultProcessingQueue.TryTake( out processItem ) )
						currentBatch.Enqueue( processItem );

					//Process the entire batch - don't observe 
					//	cancellation token
					await ProcessResultBatchAsync( currentBatch );
				}
				catch ( OperationCanceledException )
				{
					mLogger.Debug( "Cancellation requested. Breaking result processing loop..." );

					//Best effort to cancel all tasks
					foreach ( PostgreSqlTaskResultQueueProcessRequest rq in mResultProcessingQueue.ToArray() )
						rq.SetCancelled();

					break;
				}
				catch ( Exception exc )
				{
					//Add them back to processing queue to be retried
					foreach ( PostgreSqlTaskResultQueueProcessRequest rq in currentBatch )
					{
						rq.SetFailed( exc );
						if ( rq.CanBeRetried )
							mResultProcessingQueue.Add( rq );
					}

					currentBatch.Clear();
					mLogger.Error( "Error processing results", exc );
				}
				finally
				{
					//Clear batch and start over
					currentBatch.Clear();
				}
			}
		}

		private async Task StartProcessingAsync()
		{
			await PrepConnectionPoolAsync( CancellationToken.None );

			mResultProcessingStopRequest = new CancellationTokenSource();
			mResultProcessingQueue = new BlockingCollection<PostgreSqlTaskResultQueueProcessRequest>();

			mResultProcessingTask = Task.Run( async ()
				=> await RunProcessingLoopAsync() );
		}

		public async Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await StartProcessingAsync() );
			else
				mLogger.Debug( "Result queue is already started or in the process of starting." );
		}

		private async Task StopProcessingAsync()
		{
			mResultProcessingQueue.CompleteAdding();
			mResultProcessingStopRequest.Cancel();
			await mResultProcessingTask;

			mResultProcessingQueue.Dispose();
			mResultProcessingStopRequest.Dispose();

			mResultProcessingQueue = null;
			mResultProcessingStopRequest = null;
			mResultProcessingTask = null;
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopAsync( async ()
					=> await StopProcessingAsync() );
			else
				mLogger.Debug( "Result queue is already stopped or in the process of stopping." );
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					mStateController = null;
				}

				mIsDisposed = true;
			}
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

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
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
