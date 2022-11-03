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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskResultQueue : ITaskResultQueue, IAppMetricsProvider, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public event EventHandler<TaskResultProcessedEventArgs> TaskResultProcessed;

		private readonly TaskQueueOptions mOptions;

		private readonly ITaskResultQueueMetricsProvider mMetricsProvider;

		private readonly AsyncProcessingRequestBatchProcessor<ResultQueueProcessingRequest> mBatchProcessor;

		private readonly string mUpdateSql;

		private long mLastRequestId = 0;

		private bool mIsDisposed = false;

		public PostgreSqlTaskResultQueue( TaskQueueOptions options,
			ITaskResultQueueMetricsProvider metricsProvider )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );

			mUpdateSql = BuildUpdateSql( options.Mapping );
			mBatchProcessor = new AsyncProcessingRequestBatchProcessor<ResultQueueProcessingRequest>( ProcessRequestBatchAsync, mLogger );
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
			mMetricsProvider.IncrementPostResultCount();
		}

		private void IncrementResultWriteCount( TimeSpan duration )
		{
			mMetricsProvider.IncrementResultWriteCount( duration );
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

		public async Task PostResultAsync( IQueuedTaskResult result )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			ResultQueueProcessingRequest processRequest =
				new ResultQueueProcessingRequest( GenerateRequestId(),
					result,
					timeoutMilliseconds: 0,
					maxFailCount: 3 );

			await mBatchProcessor.PostRequestAsync( processRequest );
		}

		private long GenerateRequestId()
		{
			return Interlocked.Increment( ref mLastRequestId );
		}

		public async Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			if ( !mBatchProcessor.IsRunning )
				await StartProcessingAsync();
			else
				mLogger.Debug( "Result queue is already started or in the process of starting." );
		}

		private async Task StartProcessingAsync()
		{
			await PrepConnectionPoolAsync( CancellationToken.None );
			await mBatchProcessor.StartAsync();
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

						OnTaskResultProcessed( processRq
							.ResultToUpdate );
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
							await mBatchProcessor.PostRequestAsync( processRq );

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

		private void OnTaskResultProcessed( IQueuedTaskResult result )
		{
			EventHandler<TaskResultProcessedEventArgs> eventHandler = TaskResultProcessed;
			if ( eventHandler != null )
				eventHandler( this, new TaskResultProcessedEventArgs( result ) );
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mBatchProcessor.IsRunning )
				await StopProcessingAsync();
			else
				mLogger.Debug( "Result queue is already stopped or in the process of stopping." );
		}

		private async Task StopProcessingAsync()
		{
			await mBatchProcessor.StopAsync();
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					mBatchProcessor.Dispose();
				}

				mIsDisposed = true;
			}
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetricsProvider.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetricsProvider.CollectMetrics();
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
				return mMetricsProvider.ExportedMetrics;
			}
		}
	}
}
