using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Npgsql;
using NpgsqlTypes;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueTimingBelt : ITaskQueueTimingBelt, IDisposable
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private string mTimeConnectionString;

		private int mTimeTickBatchSize;

		private int mTimeTickRequestMaxFailCount = 3;

		private Guid mTimeId;

		private long mLocalWallclockTimeCostSinceLastTick;

		private long mTotalLocalWallclockTimeCost;

		private AbstractTimestamp mLastTime;

		private StateController mStateController =
			new StateController();

		private CancellationTokenSource mTimeTickingStopRequest;

		private BlockingCollection<PostgreSqlTaskQueueTimingBeltTickRequest> mTickingQueue;

		private Task mTimeTickingTask;

		private bool mIsDisposed = false;

		public PostgreSqlTaskQueueTimingBelt ( Guid timeId, string timeConnectionString, int timeTickBatchSize, int timeTickMaxFailCount )
		{
			mTimeId = timeId;
			mTimeConnectionString = timeConnectionString;
			mTimeTickBatchSize = timeTickBatchSize;
			mTimeTickRequestMaxFailCount = 3;

			mLastTime = AbstractTimestamp.Zero();
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueTimingBelt ),
					"Cannot reuse a disposed postgre sql task queue timing belt" );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ( CancellationToken cancellationToken )
		{
			NpgsqlConnection conn = new NpgsqlConnection( mTimeConnectionString );
			await conn.OpenAsync( cancellationToken );
			return conn;
		}

		private async Task<AbstractTimestamp> TickAsync ( int ticksCount, CancellationToken cancellationToken )
		{
			string tickSql = @"UPDATE sk_time_t 
				SET t_total_ticks = t_total_ticks + @t_add_ticks, 
					t_total_ticks_cost = t_total_ticks_cost + @t_last_cost 
				WHERE t_id = @t_id
				RETURNING t_total_ticks AS new_t_total_ticks, 
					t_total_ticks_cost AS new_t_total_ticks_cost";

			double tickLastCost = Interlocked.Exchange( ref mLocalWallclockTimeCostSinceLastTick, 0 );

			using ( NpgsqlConnection tickConn = await OpenConnectionAsync( cancellationToken ) )
			using ( NpgsqlCommand tickCmd = new NpgsqlCommand( tickSql, tickConn ) )
			{
				tickCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					mTimeId );
				tickCmd.Parameters.AddWithValue( "t_add_ticks",
					NpgsqlDbType.Bigint,
					ticksCount );
				tickCmd.Parameters.AddWithValue( "t_last_cost",
					NpgsqlDbType.Bigint,
					tickLastCost );

				using ( NpgsqlDataReader rdr = await tickCmd.ExecuteReaderAsync( cancellationToken ) )
				{
					long newTotalTicks = rdr.GetInt64( rdr.GetOrdinal(
						"new_t_total_ticks"
					) );

					long newTotalTicksCost = rdr.GetInt64( rdr.GetOrdinal(
						"new_t_total_ticks_cost"
					) );

					AbstractTimestamp lastTime = new AbstractTimestamp( newTotalTicks,
						newTotalTicksCost );

					Interlocked.Exchange( ref mLastTime,
						lastTime );

					return lastTime;
				}
			}
		}

		private void StartTimeTickingTask ()
		{
			mTickingQueue = new BlockingCollection<PostgreSqlTaskQueueTimingBeltTickRequest>();
			mTimeTickingStopRequest = new CancellationTokenSource();

			mTimeTickingTask = Task.Run( async () =>
			{
				CancellationToken cancellationToken = mTimeTickingStopRequest
				   .Token;

				while ( !mTickingQueue.IsCompleted )
				{
					List<PostgreSqlTaskQueueTimingBeltTickRequest> currentBatch =
						new List<PostgreSqlTaskQueueTimingBeltTickRequest>();

					try
					{
						cancellationToken.ThrowIfCancellationRequested();

						//Try to dequeue and block if no item is available
						PostgreSqlTaskQueueTimingBeltTickRequest tickRqBatchItem = 
							mTickingQueue.Take( cancellationToken );
						
						currentBatch.Add( tickRqBatchItem );

						//See if there are other items available 
						//	and add them to current batch
						while ( mTickingQueue.TryTake( out tickRqBatchItem )
							&& currentBatch.Count < mTimeTickBatchSize )
							currentBatch.Add( tickRqBatchItem );

						cancellationToken.ThrowIfCancellationRequested();

						//Tick the entire batch
						AbstractTimestamp lastTime = await TickAsync( currentBatch.Count, 
							cancellationToken );

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

						mLogger.Error( "Error processing time tick", exc );
						currentBatch.Clear();
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

		public Task StartAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				mStateController.TryRequestStart( () => StartTimeTickingTask() );
			else
				mLogger.Debug( "Timing belt is already started or in the process of starting." );

			return Task.CompletedTask;
		}

		public async Task StopAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async () => await StopTimeTickingTask() );
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

			AbstractTimestamp lastTime = mLastTime.Copy();

			TaskCompletionSource<AbstractTimestamp> completionToken =
				new TaskCompletionSource<AbstractTimestamp>();

			PostgreSqlTaskQueueTimingBeltTickRequest tickRequest =
				new PostgreSqlTaskQueueTimingBeltTickRequest( completionToken,
					timeoutMilliseconds, mTimeTickRequestMaxFailCount );

			mTickingQueue.Add( tickRequest );

			return completionToken.Task.ContinueWith( ( prev ) =>
			{
				tickRequest.Dispose();
				return prev.Result ?? lastTime;
			} );
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
	}
}
