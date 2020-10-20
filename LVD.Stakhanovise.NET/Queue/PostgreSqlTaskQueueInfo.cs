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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueInfo : ITaskQueueInfo
	{
		private TaskQueueInfoOptions mOptions;

		private bool mIsDisposed = false;

		private ITimestampProvider mTimestampProvider;

		public PostgreSqlTaskQueueInfo ( TaskQueueInfoOptions options, ITimestampProvider timestampProvider )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );
			if ( timestampProvider == null )
				throw new ArgumentNullException( nameof( timestampProvider ) );

			mOptions = options;
			mTimestampProvider = timestampProvider;
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueInfo ),
					"Cannot reuse a disposed task queue info" );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
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

			string statsResultsSql = $@"SELECT q.task_status, 
					COUNT(q.task_status) AS task_status_count 
				FROM {mOptions.Mapping.ResultsQueueTableName} AS q 
				GROUP BY q.task_status";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand statsResultsCmd = new NpgsqlCommand( statsResultsSql, conn ) )
			using ( NpgsqlDataReader statsResultsRdr = await statsResultsCmd.ExecuteReaderAsync() )
			{
				while ( statsResultsRdr.Read() )
				{
					long count = await statsResultsRdr.GetFieldValueAsync( "task_status_count",
						defaultValue: 0 );

					QueuedTaskStatus status = ( QueuedTaskStatus )( await statsResultsRdr.GetFieldValueAsync( "task_status",
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

				await statsResultsRdr.CloseAsync();
				await conn.CloseAsync();
			}

			return new TaskQueueMetrics( totalUnprocessed,
				totalProcessing,
				totalErrored,
				totalFaulted,
				totalFataled,
				totalProcessed );
		}

		public async Task<IQueuedTask> PeekAsync ()
		{
			IQueuedTask peekedTask = null;
			DateTimeOffset refNow = mTimestampProvider.GetNow();

			CheckNotDisposedOrThrow();

			//This simply returns the latest item on top of the queue,
			//  without acquiring any lock

			string peekSql = $@"SELECT q.*
				FROM {mOptions.Mapping.QueueTableName} as q
				WHERE q.task_locked_until_ts < @t_now
					AND sk_has_advisory_lock(q.task_lock_handle_id) = false
				ORDER BY q.task_priority DESC,
					q.task_locked_until_ts ASC,
					q.task_lock_handle_id ASC
				LIMIT 1";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand peekCmd = new NpgsqlCommand( peekSql, conn ) )
			{
				peekCmd.Parameters.AddWithValue( "t_now",
					NpgsqlDbType.TimestampTz,
					refNow );

				await peekCmd.PrepareAsync();
				using ( NpgsqlDataReader taskReader = await peekCmd.ExecuteReaderAsync() )
				{
					if ( await taskReader.ReadAsync() )
						peekedTask = await taskReader.ReadQueuedTaskAsync();

					await taskReader.CloseAsync();
				}

				await conn.CloseAsync();
			}

			return peekedTask;
		}

		public ITimestampProvider TimestampProvider
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mTimestampProvider;
			}
		}
	}
}
