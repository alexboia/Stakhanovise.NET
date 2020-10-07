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
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskResultQueue : ITaskResultQueue, IDisposable
	{
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

		private long mLastRequestId;

		public PostgreSqlTaskResultQueue ( TaskQueueOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskResultQueue ),
					"Cannot reuse a disposed postgre sql task result queue" );
		}

		private void CheckRunningOrThrow ()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The task result queue is not running." );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ( CancellationToken cancellationToken )
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync( cancellationToken );
		}

		private async Task PrepConnectionPoolAsync ( CancellationToken cancellationToken )
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync( cancellationToken ) )
				await conn.CloseAsync();
		}

		public Task PostResultAsync ( IQueuedTaskToken token, int timeoutMilliseconds )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			if ( token == null )
				throw new ArgumentNullException( nameof( token ) );

			long requestId = Interlocked.Increment( ref mLastRequestId );

			TaskCompletionSource<int> completionToken =
				new TaskCompletionSource<int>( TaskCreationOptions
					.RunContinuationsAsynchronously );

			PostgreSqlTaskResultQueueProcessRequest processRequest =
				new PostgreSqlTaskResultQueueProcessRequest( requestId,
					token.LastQueuedTaskResult,
					completionToken,
					timeoutMilliseconds: timeoutMilliseconds,
					maxFailCount: 3 );

			mResultProcessingQueue.Add( processRequest );

			return completionToken.Task.WithCleanup( ( prev )
				=> processRequest.Dispose() );
		}

		public Task PostResultAsync ( IQueuedTaskToken token )
		{
			return PostResultAsync( token, 
				timeoutMilliseconds: 0 );
		}

		private async Task ProcessResultBatchAsync ( Queue<PostgreSqlTaskResultQueueProcessRequest> currentBatch,
			CancellationToken cancellationToken )
		{
			//An explicit choice has been made not to use transactions
			//	since failing to update a result MUST NOT 
			//	cause the other successful updates to be rolled back.
			using ( NpgsqlConnection conn = await OpenConnectionAsync( cancellationToken ) )
			{
				string updateSql = $@"UPDATE {mOptions.Mapping.ResultsTableName} SET 
						task_status = @t_status,
						task_last_error = @t_last_error,
						task_error_count = @t_error_count,
						task_last_error_recoverable = @t_last_error_recoverable,
						task_processing_time_milliseconds = @t_processing_time_milliseconds,
						task_processing_finalized_at_ts = @t_processing_finalized_at_ts
					WHERE task_id = @t_id";

				using ( NpgsqlCommand updateCmd = new NpgsqlCommand( updateSql, conn ) )
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
						.Add( "t_processing_finalied_at_ts", NpgsqlDbType.TimestampTz );
					NpgsqlParameter pId = updateCmd.Parameters
						.Add( "t_id", NpgsqlDbType.Uuid );

					await updateCmd.PrepareAsync( cancellationToken );

					while ( currentBatch.Count > 0 )
					{
						PostgreSqlTaskResultQueueProcessRequest processRq =
							currentBatch.Dequeue();

						try
						{
							pStatus.Value = ( int )processRq.ResultToUpdate.Status;
							pLastError.Value = processRq.ResultToUpdate.LastError.ToJson();
							pErrorCount.Value = processRq.ResultToUpdate.ErrorCount;
							pLastErrorIsRecoverable.Value = processRq.ResultToUpdate.LastErrorIsRecoverable;
							pProcessingTime.Value = processRq.ResultToUpdate.ProcessingTimeMilliseconds;
							pFinalizedAt.Value = processRq.ResultToUpdate.ProcessingFinalizedAtTs;
							pId.Value = processRq.ResultToUpdate.Id;

							int affectedRows = await updateCmd.ExecuteNonQueryAsync( cancellationToken );
							processRq.SetCompleted( affectedRows );
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
				}

				await conn.CloseAsync();
			}
		}

		private async Task StartProcessingAsync ()
		{
			await PrepConnectionPoolAsync( CancellationToken.None );

			mResultProcessingStopRequest = new CancellationTokenSource();
			mResultProcessingQueue = new BlockingCollection<PostgreSqlTaskResultQueueProcessRequest>();

			mResultProcessingTask = Task.Run( async () =>
			 {
				 CancellationToken stopToken = mResultProcessingStopRequest
					.Token;

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
						 while ( currentBatch.Count < 5 && mResultProcessingQueue.TryTake( out processItem ) )
							 currentBatch.Enqueue( processItem );

						 stopToken.ThrowIfCancellationRequested();

						 //Process the entire batch
						 await ProcessResultBatchAsync( currentBatch,
							 stopToken );

						 //Clear batch and start over
						 currentBatch.Clear();
					 }
					 catch ( OperationCanceledException )
					 {
						 //Best effort to cancel all tasks - both the current batch
						 //	 and the remaining items in queue
						 foreach ( PostgreSqlTaskResultQueueProcessRequest rq in currentBatch )
							 rq.SetCancelled();

						 foreach ( PostgreSqlTaskResultQueueProcessRequest rq in mResultProcessingQueue.ToArray() )
							 rq.SetCancelled();

						 currentBatch.Clear();
						 mLogger.Debug( "Cancellation requested. Breaking result processing loop..." );

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
				 }
			 } );
		}

		public async Task StartAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await StartProcessingAsync() );
			else
				mLogger.Debug( "Result queue is already started or in the process of starting." );
		}

		private async Task StopProcessingAsync ()
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

		public async Task StopAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async ()
					=> await StopProcessingAsync() );
			else
				mLogger.Debug( "Result queue is already stopped or in the process of stopping." );
		}

		protected virtual void Dispose ( bool disposing )
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
