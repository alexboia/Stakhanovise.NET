// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
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
	public class PostgreSqlAppMetricsMonitorWriter : IAppMetricsMonitorWriter
	{
		private PostgreSqlAppMetricsMonitorWriterOptions mOptions;

		private string mMetricsUpsertSql;


		public PostgreSqlAppMetricsMonitorWriter( PostgreSqlAppMetricsMonitorWriterOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mMetricsUpsertSql = GetMetricsUpsertSql( options.Mapping );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync()
		{
			return await mOptions.ConnectionOptions.TryOpenConnectionAsync();
		}

		private string GetMetricsUpsertSql( QueuedTaskMapping mapping )
		{
			return $@"INSERT INTO {mapping.MetricsTableName} (
					metric_id,
					metric_owner_process_id,
					metric_category,
					metric_value,
					metric_last_updated
				) VALUES (
					@m_id,
					@m_owner_process_id,
					@m_category,
					@m_value,
					NOW()
				) ON CONFLICT (metric_id, metric_owner_process_id) DO UPDATE SET 
					metric_category = EXCLUDED.metric_category,
					metric_value = EXCLUDED.metric_value,
					metric_last_updated = NOW()";
		}

		public async Task<int> WriteAsync( string processId, IEnumerable<AppMetric> appMetrics )
		{
			int affectedRows = 0;

			if ( appMetrics == null )
				throw new ArgumentNullException( nameof( appMetrics ) );

			if ( appMetrics.Count() == 0 )
				return affectedRows;

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			using ( NpgsqlCommand upsertCmd = new NpgsqlCommand( mMetricsUpsertSql, conn, tx ) )
			{
				try
				{
					NpgsqlParameter pMetricId = upsertCmd.Parameters.Add( "m_id",
						NpgsqlDbType.Varchar );
					NpgsqlParameter pMetricOwnerProcessId = upsertCmd.Parameters.Add( "m_owner_process_id",
						NpgsqlDbType.Varchar );
					NpgsqlParameter pMetricCategory = upsertCmd.Parameters.Add( "m_category",
						NpgsqlDbType.Varchar );
					NpgsqlParameter pMetricValue = upsertCmd.Parameters.Add( "m_value",
						NpgsqlDbType.Bigint );

					await upsertCmd.PrepareAsync();

					foreach ( AppMetric m in appMetrics )
					{
						pMetricId.Value = m.Id.ValueId;
						pMetricOwnerProcessId.Value = processId;
						pMetricCategory.Value = m.Id.ValueCategory;
						pMetricValue.Value = m.Value;

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
