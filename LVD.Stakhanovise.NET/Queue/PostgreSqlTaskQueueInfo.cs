using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;
using System.Linq;
using NpgsqlTypes;
using System.Net.Http.Headers;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueInfo : ITaskQueueInfo
	{
		private string mReadOnlyConnectionString;

		private bool mIsDisposed = false;

		private TaskQueueOptions mOptions;

		private int[] mDequeueWithStatuses;

		public PostgreSqlTaskQueueInfo ( TaskQueueOptions options )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			mOptions = options;

			mDequeueWithStatuses = mOptions.DequeueWithStatuses
				.Select( s => ( int )s )
				.ToArray();
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueInfo ),
					"Cannot reuse a disposed task queue info" );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			return await mReadOnlyConnectionString.TryOpenConnectionAsync( mOptions.ConnectionRetryCount,
				mOptions.ConnectionRetryDelay );
		}

		public async Task<TaskQueueMetrics> ComputeMetricsAsync ()
		{
			long totalUnprocessed = 0,
				totalProcessing = 0,
				totalErrored = 0,
				totalFaulted = 0,
				totalFataled = 0,
				totalProcessed = 0;

			CheckNotDisposedOrThrow();

			string statsSql = $@"SELECT q.{mOptions.Mapping.StatusColumnName},
					COUNT(q.{mOptions.Mapping.StatusColumnName}) AS task_status_count 
				FROM {mOptions.Mapping.TableName} AS q 
				GROUP BY q.{mOptions.Mapping.StatusColumnName}";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand statsCmd = new NpgsqlCommand( statsSql, conn ) )
			using ( NpgsqlDataReader statsReader = await statsCmd.ExecuteReaderAsync() )
			{
				while ( statsReader.Read() )
				{
					long count = await statsReader.GetFieldValueAsync( "task_status_count",
						defaultValue: 0 );

					QueuedTaskStatus status = ( QueuedTaskStatus )( await statsReader.GetFieldValueAsync( mOptions.Mapping.StatusColumnName,
						defaultValue: 0 ) );

					switch ( status )
					{
						case QueuedTaskStatus.Unprocessed:
							totalUnprocessed = count;
							break;
						case QueuedTaskStatus.Processing:
							totalProcessing = count;
							break;
						case QueuedTaskStatus.Error:
							totalErrored = count;
							break;
						case QueuedTaskStatus.Faulted:
							totalFaulted = count;
							break;
						case QueuedTaskStatus.Fatal:
							totalFataled = count;
							break;
						case QueuedTaskStatus.Processed:
							totalProcessed = count;
							break;
					}
				}

				await statsReader.CloseAsync();
				await conn.CloseAsync();
			}

			return new TaskQueueMetrics( totalUnprocessed,
				totalProcessing,
				totalErrored,
				totalFaulted,
				totalFataled,
				totalProcessed );
		}

		public async Task<IQueuedTask> PeekAsync ( AbstractTimestamp now )
		{
			IQueuedTask peekedTask = null;

			if ( now == null )
				throw new ArgumentNullException( nameof( now ) );

			CheckNotDisposedOrThrow();

			//This simply returns the latest item on top of the queue,
			//  without acquiring any lock

			string peekSql = $@"SELECT q.*
				FROM {mOptions.Mapping.TableName} as q
				WHERE {mOptions.Mapping.StatusColumnName} = ANY (@t_select_statuses)
					AND q.{mOptions.Mapping.LockedUntilColumnName} < @t_now
					AND sk_has_advisory_lock(q.{mOptions.Mapping.LockHandleIdColumnName}) = false
				ORDER BY q.{mOptions.Mapping.PriorityColumnName} DESC,
					q.{mOptions.Mapping.PostedAtColumnName},
					q.{mOptions.Mapping.LockHandleIdColumnName} 
				LIMIT 1";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand peekCmd = new NpgsqlCommand( peekSql, conn ) )
			{
				peekCmd.Parameters.AddWithValue( "t_select_statuses",
					NpgsqlDbType.Array | NpgsqlDbType.Integer,
					mDequeueWithStatuses );
				peekCmd.Parameters.AddWithValue( "t_now",
					NpgsqlDbType.Bigint,
					now.Ticks );

				peekCmd.Prepare();

				using ( NpgsqlDataReader taskReader = await peekCmd.ExecuteReaderAsync() )
				{
					if ( await taskReader.ReadAsync() )
						peekedTask = await taskReader.ReadQueuedTaskAsync( mOptions.Mapping );

					await taskReader.CloseAsync();
				}

				await conn.CloseAsync();
			}

			return peekedTask;
		}
	}
}
