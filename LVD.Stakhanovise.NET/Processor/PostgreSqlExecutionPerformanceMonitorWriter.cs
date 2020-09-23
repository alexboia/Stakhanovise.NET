using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class PostgreSqlExecutionPerformanceMonitorWriter : IExecutionPerformanceMonitorWriter
	{
		private PostgreSqlExecutionPerformanceMonitorWriterOptions mOptions;


		public PostgreSqlExecutionPerformanceMonitorWriter ( PostgreSqlExecutionPerformanceMonitorWriterOptions options )
		{
			mOptions = options ?? throw new ArgumentNullException( nameof( options ) );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			return await mOptions.ConnectionString.TryOpenConnectionAsync( mOptions.ConnectionRetryCount,
				mOptions.ConnectionRetryDelay );
		}

		public async Task SetupIfNeededAsync ()
		{
			string createTableSql = @"CREATE TABLE IF NOT EXISTS public.sk_task_execution_time_stats_t
				(
					et_payload_type character varying(255) NOT NULL,
					et_n_execution_cycles bigint NOT NULL,
					et_last_execution_time bigint NOT NULL,
					et_avg_execution_time bigint NOT NULL,
					et_fastest_execution_time bigint NOT NULL,
					et_longest_execution_time bigint NOT NULL,
					et_total_execution_time bigint NOT NULL,
					CONSTRAINT sk_task_execution_time_stats_t_pkey PRIMARY KEY ( et_payload_type)
				)";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand createTableCmd = new NpgsqlCommand( createTableSql, conn ) )
			{
				await createTableCmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		public async Task WriteAsync ( IReadOnlyDictionary<string, TaskExecutionStats> executionTimeInfo )
		{
			if ( executionTimeInfo == null )
				throw new ArgumentNullException( nameof( executionTimeInfo ) );

			if ( executionTimeInfo.Count == 0 )
				return;

			string upsertSql = @"INSERT INTO sk_task_execution_time_stats_t (
					et_payload_type,
					et_n_execution_cycles,
					et_last_execution_time,
					et_avg_execution_time,
					et_fastest_execution_time,
					et_longest_execution_time,
					et_total_execution_time
				) VALUES (
					@payload_type,
					@n_execution_cycles,
					@last_execution_time,
					@avg_execution_time,
					@fastest_execution_time,
					@longest_execution_time,
					@total_execution_time
				) ON CONFLICT (et_payload_type) DO UPDATE 
					et_last_execution_time = EXCLUDED.et_last_execution_time,
					et_avg_execution_time = CEILING(
						(et_total_execution_time + EXCLUDED.et_total_execution_time)::double precision 
							/ (et_n_execution_cycles + EXCLUDED.et_n_execution_cycles)
					)::bigint,
					et_fastest_execution_time = LEAST(et_fastest_execution_time, EXCLUDED.et_fastest_execution_time),
					et_longest_execution_time = GREATEST(et_longest_execution_time, EXCLUDED.et_longest_execution_time),
					et_total_execution_time = et_total_execution_time + EXCLUDED.et_total_execution_time,
					et_n_execution_cycles = et_n_execution_cycles + EXCLUDED.et_n_execution_cycles";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			using ( NpgsqlCommand upsertCmd = new NpgsqlCommand( upsertSql, conn, tx ) )
			{
				try
				{
					upsertCmd.Parameters.Add( "payload_type", NpgsqlDbType.Varchar );
					upsertCmd.Parameters.Add( "n_execution_cycles", NpgsqlDbType.Bigint );
					upsertCmd.Parameters.Add( "last_execution_time", NpgsqlDbType.Bigint );
					upsertCmd.Parameters.Add( "avg_execution_time", NpgsqlDbType.Bigint );
					upsertCmd.Parameters.Add( "fastest_execution_time", NpgsqlDbType.Bigint );
					upsertCmd.Parameters.Add( "longest_execution_time", NpgsqlDbType.Bigint );
					upsertCmd.Parameters.Add( "total_execution_time", NpgsqlDbType.Bigint );

					await upsertCmd.PrepareAsync();

					foreach ( KeyValuePair<string, TaskExecutionStats> payloadInfo in executionTimeInfo )
					{
						upsertCmd.Parameters[ "payload_type" ].Value = payloadInfo.Key;
						upsertCmd.Parameters[ "n_execution_cycles" ].Value = payloadInfo.Value
							.NumberOfExecutionCycles;
						upsertCmd.Parameters[ "last_execution_time" ].Value = payloadInfo.Value
							.LastExecutionTime;
						upsertCmd.Parameters[ "avg_execution_time" ].Value = payloadInfo.Value
							.AverageExecutionTime;
						upsertCmd.Parameters[ "fastest_execution_time" ].Value = payloadInfo.Value
							.FastestExecutionTime;
						upsertCmd.Parameters[ "longest_execution_time" ].Value = payloadInfo.Value
							.LongestExecutionTime;
						upsertCmd.Parameters[ "total_execution_time" ].Value = payloadInfo.Value
							.TotalExecutionTime;

						await upsertCmd.ExecuteNonQueryAsync();
					}

					await tx.CommitAsync();
				}
				catch ( Exception )
				{
					await tx.RollbackAsync();
					throw;
				}
			}
		}
	}
}
