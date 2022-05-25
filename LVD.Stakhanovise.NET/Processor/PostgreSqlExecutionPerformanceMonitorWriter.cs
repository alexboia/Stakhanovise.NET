// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class PostgreSqlExecutionPerformanceMonitorWriter : IExecutionPerformanceMonitorWriter
	{
		private PostgreSqlExecutionPerformanceMonitorWriterOptions mOptions;

		private string mPerfMonInfoUpsertSql;

		public PostgreSqlExecutionPerformanceMonitorWriter( PostgreSqlExecutionPerformanceMonitorWriterOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );

			mPerfMonInfoUpsertSql = GetPerfMonInfoUpsertSql( options.Mapping );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync()
		{
			return await mOptions.ConnectionOptions.TryOpenConnectionAsync();
		}

		private string GetPerfMonInfoUpsertSql( QueuedTaskMapping mapping )
		{
			return $@"INSERT INTO {mapping.ExecutionTimeStatsTableName} (
					et_payload_type,
					et_owner_process_id,
					et_n_execution_cycles,
					et_last_execution_time,
					et_avg_execution_time,
					et_fastest_execution_time,
					et_longest_execution_time,
					et_total_execution_time
				) VALUES (
					@payload_type,
					@owner_process_id,
					@n_execution_cycles,
					@last_execution_time,
					@avg_execution_time,
					@fastest_execution_time,
					@longest_execution_time,
					@total_execution_time
				) ON CONFLICT (et_payload_type, et_owner_process_id) DO UPDATE SET 
					et_last_execution_time = EXCLUDED.et_last_execution_time,
					et_avg_execution_time = CEILING(
						({mapping.ExecutionTimeStatsTableName}.et_total_execution_time + EXCLUDED.et_last_execution_time)::double precision 
							/ ({mapping.ExecutionTimeStatsTableName}.et_n_execution_cycles + 1)
					)::bigint,
					et_fastest_execution_time = LEAST({mapping.ExecutionTimeStatsTableName}.et_fastest_execution_time, 
						EXCLUDED.et_last_execution_time),
					et_longest_execution_time = GREATEST({mapping.ExecutionTimeStatsTableName}.et_longest_execution_time, 
						EXCLUDED.et_last_execution_time),
					et_total_execution_time = {mapping.ExecutionTimeStatsTableName}.et_total_execution_time 
						+ EXCLUDED.et_last_execution_time,
					et_n_execution_cycles = {mapping.ExecutionTimeStatsTableName}.et_n_execution_cycles 
						+ 1";
		}

		public async Task<int> WriteAsync( string processId, IEnumerable<TaskPerformanceStats> executionTimeInfoBatch )
		{
			int affectedRows = 0;

			if ( executionTimeInfoBatch == null )
				throw new ArgumentNullException( nameof( executionTimeInfoBatch ) );

			if ( executionTimeInfoBatch.Count() == 0 )
				return 0;

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			using ( NpgsqlCommand upsertCmd = new NpgsqlCommand( mPerfMonInfoUpsertSql, conn, tx ) )
			{
				try
				{
					NpgsqlParameter pPayloadType = upsertCmd.Parameters
					   .Add( "payload_type", NpgsqlDbType.Varchar );
					NpgsqlParameter pOwnerProcessId = upsertCmd.Parameters
						.Add( "owner_process_id", NpgsqlDbType.Varchar );
					NpgsqlParameter pNExecutionCycles = upsertCmd.Parameters
						.Add( "n_execution_cycles", NpgsqlDbType.Bigint );
					NpgsqlParameter pLastExecutionTime = upsertCmd.Parameters
						.Add( "last_execution_time", NpgsqlDbType.Bigint );
					NpgsqlParameter pAvgExecutionTime = upsertCmd.Parameters
						.Add( "avg_execution_time", NpgsqlDbType.Bigint );
					NpgsqlParameter pFastestExecutionTime = upsertCmd.Parameters
						.Add( "fastest_execution_time", NpgsqlDbType.Bigint );
					NpgsqlParameter pLongestExecutionTime = upsertCmd.Parameters
						.Add( "longest_execution_time", NpgsqlDbType.Bigint );
					NpgsqlParameter pTotalExecutionTime = upsertCmd.Parameters
						.Add( "total_execution_time", NpgsqlDbType.Bigint );

					await upsertCmd.PrepareAsync();

					foreach ( TaskPerformanceStats s in executionTimeInfoBatch )
					{
						pPayloadType.Value = s.PayloadType;
						pOwnerProcessId.Value = processId;
						pNExecutionCycles.Value = 1;
						pLastExecutionTime.Value = s.DurationMilliseconds;
						pAvgExecutionTime.Value = s.DurationMilliseconds;
						pFastestExecutionTime.Value = s.DurationMilliseconds;
						pLongestExecutionTime.Value = s.DurationMilliseconds;
						pTotalExecutionTime.Value = s.DurationMilliseconds;

						affectedRows += await upsertCmd.ExecuteNonQueryAsync();
					}

					await tx.CommitAsync();
				}
				catch ( Exception )
				{
					await tx.RollbackAsync();
					throw;
				}
			}

			return affectedRows;
		}
	}
}
