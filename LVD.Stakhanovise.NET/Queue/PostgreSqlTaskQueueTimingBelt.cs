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
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueTimingBelt : ITaskQueueTimingBelt, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private long mLocalWallclockTimeCostSinceLastTick;

		private long mTotalLocalWallclockTimeCost;

		private AbstractTimestamp mLastTime;

		private StateController mStateController =
			new StateController();

		private CancellationTokenSource mTimeTickingStopRequest;

		private BlockingCollection<PostgreSqlTaskQueueTimingBeltTickRequest> mTickingQueue;

		private Task mTimeTickingTask;

		private bool mIsDisposed = false;

		private long mLastRequestId;

		private PostgreSqlTaskQueueTimingBeltOptions mOptions;

		public PostgreSqlTaskQueueTimingBelt ( PostgreSqlTaskQueueTimingBeltOptions options )
		{

			mOptions = options 
				?? throw new ArgumentNullException( nameof( options ) );

			mTotalLocalWallclockTimeCost 
				= mLocalWallclockTimeCostSinceLastTick 
				= mOptions.InitialWallclockTimeCost;

			mLastTime = new AbstractTimestamp( 0, 
				mOptions.InitialWallclockTimeCost );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueTimingBelt ),
					"Cannot reuse a disposed postgre sql task queue timing belt" );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ( CancellationToken cancellationToken )
		{
			return await mOptions.ConnectionOptions.TryOpenConnectionAsync();
		}

		private async Task PrepConnectionPoolAsync ( CancellationToken cancellationToken )
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync( cancellationToken ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( "SELECT NULL as sk_connection_prep", conn ) )
			{
				await cmd.ExecuteNonQueryAsync( cancellationToken );
				await conn.CloseAsync();
			}
		}

		private async Task<AbstractTimestamp> TickAsync ( int ticksCount, CancellationToken cancellationToken )
		{
			string tickSql = @"UPDATE sk_time_t 
				SET t_total_ticks = t_total_ticks + @t_add_ticks, 
					t_total_ticks_cost = t_total_ticks_cost + @t_last_cost 
				WHERE t_id = @t_id
				RETURNING t_total_ticks AS new_t_total_ticks, 
					t_total_ticks_cost AS new_t_total_ticks_cost";

			AbstractTimestamp lastTime = null;
			double tickLastCost = Interlocked.Exchange( ref mLocalWallclockTimeCostSinceLastTick,
				value: 0 );

			using ( NpgsqlConnection tickConn = await OpenConnectionAsync( cancellationToken ) )
			using ( NpgsqlCommand tickCmd = new NpgsqlCommand( tickSql, tickConn ) )
			{
				tickCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					mOptions.TimeId );
				tickCmd.Parameters.AddWithValue( "t_add_ticks",
					NpgsqlDbType.Bigint,
					ticksCount );
				tickCmd.Parameters.AddWithValue( "t_last_cost",
					NpgsqlDbType.Bigint,
					tickLastCost );

				using ( NpgsqlDataReader rdr = await tickCmd.ExecuteReaderAsync( cancellationToken ) )
				{
					if ( await rdr.ReadAsync() )
					{
						long newTotalTicks = rdr.GetInt64( rdr.GetOrdinal(
							"new_t_total_ticks"
						) );

						long newTotalTicksCost = rdr.GetInt64( rdr.GetOrdinal(
							"new_t_total_ticks_cost"
						) );

						lastTime = new AbstractTimestamp( newTotalTicks,
							newTotalTicksCost );

						Interlocked.Exchange( ref mLastTime,
							lastTime );

						await rdr.CloseAsync();
					}
				}

				await tickConn.CloseAsync();
			}

			return lastTime;
		}

		private async Task StartTimeTickingTask ()
		{
			await PrepConnectionPoolAsync( CancellationToken.None );

			mTimeTickingStopRequest = new CancellationTokenSource();
			mTickingQueue = new BlockingCollection<PostgreSqlTaskQueueTimingBeltTickRequest>();

			mTimeTickingTask = Task.Run( async () =>
			{
				CancellationToken stopToken = mTimeTickingStopRequest
				   .Token;

				while ( !mTickingQueue.IsCompleted )
				{
					List<PostgreSqlTaskQueueTimingBeltTickRequest> currentBatch =
						new List<PostgreSqlTaskQueueTimingBeltTickRequest>();

					try
					{
						stopToken.ThrowIfCancellationRequested();

						//Try to dequeue and block if no item is available
						PostgreSqlTaskQueueTimingBeltTickRequest tickRqBatchItem =
							mTickingQueue.Take( stopToken );

						currentBatch.Add( tickRqBatchItem );

						//See if there are other items available 
						//	and add them to current batch
						while ( currentBatch.Count < mOptions.TimeTickBatchSize && mTickingQueue.TryTake( out tickRqBatchItem ) )
							currentBatch.Add( tickRqBatchItem );

						stopToken.ThrowIfCancellationRequested();

						//Tick the entire batch
						AbstractTimestamp lastTime = await TickAsync( currentBatch.Count,
							stopToken );

						//And distribute the result to each tick request
						foreach ( PostgreSqlTaskQueueTimingBeltTickRequest processedTickRq in currentBatch )
							processedTickRq.SetCompleted( lastTime );

						//Clear batch and start over
						currentBatch.Clear();
					}
					catch ( OperationCanceledException )
					{
						//Best effort to cancel all tasks - both the current batch
						//	 and the remaining items in queue
						foreach ( PostgreSqlTaskQueueTimingBeltTickRequest rq in currentBatch )
							rq.SetCancelled();

						foreach ( PostgreSqlTaskQueueTimingBeltTickRequest rq in mTickingQueue.ToArray() )
							rq.SetCancelled();

						currentBatch.Clear();
						mLogger.Debug( "Cancellation requested. Breaking time ticking loop..." );

						break;
					}
					catch ( Exception exc )
					{
						//Add them back to ticking queue to be retried
						foreach ( PostgreSqlTaskQueueTimingBeltTickRequest rq in currentBatch )
						{
							rq.SetFailed( exc );
							if ( rq.CanBeRetried )
								mTickingQueue.Add( rq );
						}

						currentBatch.Clear();
						mLogger.Error( "Error processing time tick", exc );
					}
				}
			} );
		}

		private async Task StopTimeTickingTask ()
		{
			mTickingQueue.CompleteAdding();
			mTimeTickingStopRequest.Cancel();
			await mTimeTickingTask;

			mTickingQueue.Dispose();
			mTimeTickingStopRequest.Dispose();

			mTickingQueue = null;
			mTimeTickingStopRequest = null;
			mTimeTickingTask = null;
		}

		public async Task StartAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async ()
					=> await StartTimeTickingTask() );
			else
				mLogger.Debug( "Timing belt is already started or in the process of starting." );
		}

		public async Task StopAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async ()
					=> await StopTimeTickingTask() );
			else
				mLogger.Debug( "Timing belt is already stopped or in the process of stopping." );
		}

		public void AddWallclockTimeCost ( long milliseconds )
		{
			CheckNotDisposedOrThrow();

			Interlocked.Add( ref mLocalWallclockTimeCostSinceLastTick,
				milliseconds );
			Interlocked.Add( ref mTotalLocalWallclockTimeCost,
				milliseconds );
		}

		public void AddWallclockTimeCost ( TimeSpan duration )
		{
			AddWallclockTimeCost( ( long )duration.TotalMilliseconds );
		}

		public Task<AbstractTimestamp> TickAbstractTimeAsync ( int timeoutMilliseconds )
		{
			CheckNotDisposedOrThrow();

			long requestId = Interlocked.Increment( ref mLastRequestId );
			AbstractTimestamp lastTime = mLastTime.Copy();

			TaskCompletionSource<AbstractTimestamp> completionToken =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest tickRequest =
				new PostgreSqlTaskQueueTimingBeltTickRequest( requestId,
					completionToken,
					timeoutMilliseconds, 
					mOptions.TimeTickMaxFailCount );

			mTickingQueue.Add( tickRequest );

			return completionToken.Task.WithCleanup( ( prev )
				=> tickRequest.Dispose() );
		}

		public async Task<long> ComputeAbsolutetTimeTicksAsync ( long timeTicksToAdd )
		{
			CheckNotDisposedOrThrow();

			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
			{
				string computeSql = "SELECT sk_compute_absolute_time_ticks(@s_time_id, @s_time_ticks_to_add)";
				using ( NpgsqlCommand computeCmd = new NpgsqlCommand( computeSql, conn ) )
				{
					computeCmd.Parameters.AddWithValue( "s_time_id", NpgsqlDbType.Uuid,
						mOptions.TimeId );
					computeCmd.Parameters.AddWithValue( "s_time_ticks_to_add", NpgsqlDbType.Bigint,
						timeTicksToAdd );

					await computeCmd.PrepareAsync();
					object result = await computeCmd.ExecuteScalarAsync();

					return result is long
						? ( long )result
						: 0;
				}
			}
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();

					mStateController = null;
					mLastTime = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public AbstractTimestamp LastTime
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mLastTime.Copy();
			}
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		public long LocalWallclockTimeCostSinceLastTick
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mLocalWallclockTimeCostSinceLastTick;
			}
		}
		public long TotalLocalWallclockTimeCost
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mTotalLocalWallclockTimeCost;
			}
		}
	}
}
