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

				{ "task_posted_at", taskData.PostedAt },
				{ "task_posted_at_ts", taskData.PostedAtTs },

				{ "task_locked_until", taskData.LockedUntil }
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

				{ "task_posted_at", resultData.PostedAt },
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
