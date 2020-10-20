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
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;
using SqlKata;
using LVD.Stakhanovise.NET.Tests.Helpers;
using NpgsqlTypes;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class PostgreSqlTaskQueueDbOperations
	{
		private string mConnectionString;

		private QueuedTaskMapping mMapping;

		public PostgreSqlTaskQueueDbOperations ( string connectionString,
			QueuedTaskMapping mapping )
		{
			mConnectionString = connectionString;
			mMapping = mapping;
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( mConnectionString );
			await db.OpenAsync();
			return db;
		}

		public async Task ClearTaskAndResultDataAsync ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.DeleteAsync();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.DeleteAsync();
			}
		}

		public async Task ClearExecutionPerformanceTimeStatsTableAsync ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( "sk_task_execution_time_stats_t" )
					.DeleteAsync();
			}
		}

		private async Task AddQueuedTaskAsync ( QueuedTask taskData,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTask = new Dictionary<string, object>()
			{
				{ "task_id", taskData.Id },
				{ "task_payload", taskData.Payload.ToJson(includeTypeInformation: true) },
				{ "task_type", taskData.Type },

				{ "task_source", taskData.Source },
				{ "task_priority", taskData.Priority },

				{ "task_posted_at_ts", taskData.PostedAtTs },
				{ "task_locked_until_ts", taskData.LockedUntilTs }
			};

			if ( tx != null )
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.InsertAsync( insertDataTask, tx );
			else
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.InsertAsync( insertDataTask );
		}

		private async Task AddQueuedTaskResultAsync ( QueuedTaskResult resultData,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTaskResult = new Dictionary<string, object>()
			{
				{ "task_id", resultData.Id },
				{ "task_payload", resultData.Payload.ToJson(includeTypeInformation: true) },
				{ "task_type", resultData.Type },

				{ "task_source", resultData.Source },
				{ "task_priority", resultData.Priority },

				{ "task_posted_at_ts", resultData.PostedAtTs },
				{ "task_status", resultData.Status },

				{ "task_processing_time_milliseconds", resultData.ProcessingTimeMilliseconds },

				{ "task_error_count", resultData.ErrorCount },
				{ "task_last_error", resultData.LastError.ToJson() },
				{ "task_last_error_is_recoverable", resultData.LastErrorIsRecoverable },

				{ "task_first_processing_attempted_at_ts", resultData.FirstProcessingAttemptedAtTs },
				{ "task_last_processing_attempted_at_ts", resultData.LastProcessingAttemptedAtTs },
				{ "task_processing_finalized_at_ts", resultData.ProcessingFinalizedAtTs }
			};

			if ( tx != null )
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.InsertAsync( insertDataTaskResult, tx );
			else
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.InsertAsync( insertDataTaskResult );
		}

		public async Task AddQueuedTaskAsync ( QueuedTask taskData )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				await AddQueuedTaskAsync( taskData, conn,
					tx: null );
			}
		}

		public async Task AddQueuedTaskResultAsync ( QueuedTaskResult resultData )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				await AddQueuedTaskResultAsync( resultData, conn,
					tx: null );
			}
		}

		public async Task InsertTaskAndResultDataAsync ( IEnumerable<Tuple<QueuedTask, QueuedTaskResult>> queuedTaskPairs )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			{
				foreach ( Tuple<QueuedTask, QueuedTaskResult> p in queuedTaskPairs )
				{
					if ( p.Item1 != null )
						await AddQueuedTaskAsync( p.Item1, conn, tx );
					if ( p.Item2 != null )
						await AddQueuedTaskResultAsync( p.Item2, conn, tx );
				}

				await tx.CommitAsync();
			}
		}

		public async Task<QueuedTask> GetQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Select( "*" )
					.Where( "task_id", "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskAsync()
						: null;
			}
		}

		public async Task<QueuedTaskResult> GetQueuedTaskResultFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.Select( "*" )
					.Where( "task_id", "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskResultAsync()
						: null;
			}
		}

		public async Task RemoveQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				await new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Where( "task_id", "=", taskId )
					.DeleteAsync();
			}
		}
	}
}
