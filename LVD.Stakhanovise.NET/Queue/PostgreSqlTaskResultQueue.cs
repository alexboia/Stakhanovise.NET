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
		private const int ProcessingBatchSize = 5;

		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public event EventHandler<TaskResultProcessedEventArgs> TaskResultProcessed;

		private bool mIsDisposed = false;

		private TaskQueueOptions mOptions;

		private Task mResultProcessingTask;

		private CancellationTokenSource mStopCoordinator;

		private BlockingCollection<ResultQueueProcessingRequest> mResultProcessingQueue;

		private StateController mStateController = new StateController();

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

			mUpdateSql = BuildUpdateSql( options.Mapping );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( PostgreSqlTaskResultQueue ),
					"Cannot reuse a disposed postgre sql task result queue"
				);
			}
		}

		private void CheckRunningOrThrow()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The task result queue is not running." );
		}

		private string BuildUpdateSql( QueuedTaskMapping mapping )
		{
			return $@"UPDATE {mapping.ResultsQueueTableName} SET 
					task_status = @t_status,
					task_last_error = @t_last_error,
					task_error_count = @t_error_count,
					task_last_error_is_recoverable = @t_last_error_recoverable,
					task_processing_time_milliseconds = @t_processing_time_milliseconds,
					task_first_processing_attempted_at_ts = @t_first_processing_attempted_at_ts,
					task_last_processing_attempted_at_ts = @t_last_processing_attempted_at_ts,
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

		public Task PostResultAsync( IQueuedTaskResult result )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			long requestId = Interlocked.Increment( ref mLastRequestId );

			ResultQueueProcessingRequest processRequest =
				new ResultQueueProcessingRequest( requestId,
					result,
					timeoutMilliseconds: 0,
					maxFailCount: 3 );

			mResultProcessingQueue.Add( processRequest );
			IncrementPostResultCount();

			return Task.CompletedTask;
		}

		private void OnTaskResultProcessed( IQueuedTaskResult result )
		{
			EventHandler<TaskResultProcessedEventArgs> eventHandler = TaskResultProcessed;
			if ( eventHandler != null )
				eventHandler( this, new TaskResultProcessedEventArgs( result ) );
		}

		private async Task RunProcessingLoopAsync()
		{
			CancellationToken stopToken = mStopCoordinator
				.Token;

			while ( !stopToken.IsCancellationRequested )
			{
				try
				{
					await ProcessNextBatchOfRequestsAsync( stopToken );
					stopToken.ThrowIfCancellationRequested();
				}
				catch ( OperationCanceledException )
				{
					mLogger.Debug( "Cancellation requested. Breaking result processing loop..." );
					break;
				}
			}

			await ProcessNextBatchOfRequestsAsync( stopToken );
		}

		private async Task ProcessNextBatchOfRequestsAsync( CancellationToken stopToken )
		{
			//We need to use a queue here - as we process the batch, 
			//	we consume each element and, in case of an error 
			//	that affects all of them, 
			//	we would fail only the remaining ones, not the ones 
			//	that have been successfully processed
			AsyncProcessingRequestBatch<ResultQueueProcessingRequest> nextBatch = null;

			try
			{
				nextBatch = ExtractNextBatchOfRequests( stopToken );
				await ProcessRequestBatchAsync( nextBatch );
			}
			catch ( Exception exc )
			{
				//Add them back to processing queue to be retried
				if ( nextBatch != null )
				{
					foreach ( ResultQueueProcessingRequest rq in nextBatch )
					{
						rq.SetFailed( exc );
						if ( rq.CanBeRetried )
							mResultProcessingQueue.Add( rq );
					}
				}

				mLogger.Error( "Error processing results",
					exc );
			}
			finally
			{
				nextBatch?.Clear();
			}
		}

		private AsyncProcessingRequestBatch<ResultQueueProcessingRequest> ExtractNextBatchOfRequests( CancellationToken stopToken )
		{
			AsyncProcessingRequestBatch<ResultQueueProcessingRequest> nextBatch =
				new AsyncProcessingRequestBatch<ResultQueueProcessingRequest>( ProcessingBatchSize );

			nextBatch.FillFrom( mResultProcessingQueue,
				stopToken );

			return nextBatch;
		}

		private async Task ProcessRequestBatchAsync( AsyncProcessingRequestBatch<ResultQueueProcessingRequest> currentBatch )
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
				NpgsqlParameter pFirstProcessingAttemptedTime = updateCmd.Parameters
					.Add( "t_first_processing_attempted_at_ts", NpgsqlDbType.TimestampTz );
				NpgsqlParameter pLastProcessingAttemptedTime = updateCmd.Parameters
					.Add( "t_last_processing_attempted_at_ts", NpgsqlDbType.TimestampTz );
				NpgsqlParameter pFinalizedAt = updateCmd.Parameters
					.Add( "t_processing_finalized_at_ts", NpgsqlDbType.TimestampTz );
				NpgsqlParameter pId = updateCmd.Parameters
					.Add( "t_id", NpgsqlDbType.Uuid );

				await updateCmd.PrepareAsync();

				while ( currentBatch.Count > 0 )
				{
					ResultQueueProcessingRequest processRq =
						currentBatch.Dequeue();

					if ( processRq.IsCompleted )
						continue;

					try
					{
						pId.Value = processRq.ResultToUpdate.Id;
						pStatus.Value = ( int ) processRq.ResultToUpdate.Status;

						string strLastError = ConvertErrorInfoToJson( processRq.ResultToUpdate.LastError );
						if ( strLastError != null )
							pLastError.Value = strLastError;
						else
							pLastError.Value = DBNull.Value;

						pErrorCount.Value = processRq.ResultToUpdate.ErrorCount;

						pLastErrorIsRecoverable.Value = processRq.ResultToUpdate
							.LastErrorIsRecoverable;
						pProcessingTime.Value = processRq.ResultToUpdate
							.ProcessingTimeMilliseconds;

						if ( processRq.ResultToUpdate.FirstProcessingAttemptedAtTs.HasValue )
							pFirstProcessingAttemptedTime.Value = processRq.ResultToUpdate
								.FirstProcessingAttemptedAtTs
								.Value;
						else
							pFirstProcessingAttemptedTime.Value =
								DBNull.Value;

						if ( processRq.ResultToUpdate.LastProcessingAttemptedAtTs.HasValue )
							pLastProcessingAttemptedTime.Value = processRq.ResultToUpdate
								.LastProcessingAttemptedAtTs
								.Value;
						else
							pLastProcessingAttemptedTime.Value =
								DBNull.Value;

						if ( processRq.ResultToUpdate.ProcessingFinalizedAtTs.HasValue )
							pFinalizedAt.Value = processRq.ResultToUpdate
								.ProcessingFinalizedAtTs
								.Value;
						else
							pFinalizedAt.Value = DBNull
								.Value;

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
			return error.ToJson( mOptions.SerializerOptions.OnConfigureSerializerSettings );
		}

		private async Task StartProcessingAsync()
		{
			await PrepConnectionPoolAsync( CancellationToken.None );

			mStopCoordinator =
				new CancellationTokenSource();
			mResultProcessingQueue =
				new BlockingCollection<ResultQueueProcessingRequest>();
			mResultProcessingTask =
				Task.Run( RunProcessingLoopAsync );
		}

		public async Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( StartProcessingAsync );
			else
				mLogger.Debug( "Result queue is already started or in the process of starting." );
		}

		private async Task StopProcessingAsync()
		{
			mResultProcessingQueue.CompleteAdding();
			mStopCoordinator.Cancel();
			await mResultProcessingTask;

			mResultProcessingQueue.Dispose();
			mStopCoordinator.Dispose();

			mResultProcessingQueue = null;
			mStopCoordinator = null;
			mResultProcessingTask = null;
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopAsync( StopProcessingAsync );
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
				return mStateController.IsStarted;
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
