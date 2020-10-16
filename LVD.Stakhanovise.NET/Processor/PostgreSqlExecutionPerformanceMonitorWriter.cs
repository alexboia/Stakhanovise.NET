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
			mOptions = options 
				?? throw new ArgumentNullException( nameof( options ) );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			return await mOptions.ConnectionOptions.TryOpenConnectionAsync();
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

			//TODO: adjust to avoid division by 0 when calculating average
			//	situation may occur when nothing happens since system startup, but data is flushed anyway
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
				) ON CONFLICT (et_payload_type) DO UPDATE SET 
					et_last_execution_time = EXCLUDED.et_last_execution_time,
					et_avg_execution_time = CEILING(
						(sk_task_execution_time_stats_t.et_total_execution_time + EXCLUDED.et_total_execution_time)::double precision 
							/ (sk_task_execution_time_stats_t.et_n_execution_cycles + EXCLUDED.et_n_execution_cycles)
					)::bigint,
					et_fastest_execution_time = LEAST(sk_task_execution_time_stats_t.et_fastest_execution_time, 
						EXCLUDED.et_fastest_execution_time),
					et_longest_execution_time = GREATEST(sk_task_execution_time_stats_t.et_longest_execution_time, 
						EXCLUDED.et_longest_execution_time),
					et_total_execution_time = sk_task_execution_time_stats_t.et_total_execution_time 
						+ EXCLUDED.et_total_execution_time,
					et_n_execution_cycles = sk_task_execution_time_stats_t.et_n_execution_cycles 
						+ EXCLUDED.et_n_execution_cycles";

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
