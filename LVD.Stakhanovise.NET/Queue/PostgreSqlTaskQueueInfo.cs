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
			mReadOnlyConnectionString = mOptions.ConnectionString;
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
