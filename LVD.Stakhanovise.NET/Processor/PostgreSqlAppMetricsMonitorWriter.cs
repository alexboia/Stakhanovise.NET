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
	public class PostgreSqlAppMetricsMonitorWriter : IAppMetricsMonitorWriter
	{
		private PostgreSqlAppMetricsMonitorWriterOptions mOptions;

		private string mMetricsUpsertSql;


		public PostgreSqlAppMetricsMonitorWriter ( PostgreSqlAppMetricsMonitorWriterOptions options )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mMetricsUpsertSql = GetMetricsUpsertSql( options.Mapping );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			return await mOptions.ConnectionOptions.TryOpenConnectionAsync();
		}

		private string GetMetricsUpsertSql ( QueuedTaskMapping mapping )
		{
			return $@"INSERT INTO {mapping.MetricsTableName} (
					metric_id,
					metric_category,
					metric_value,
					metric_last_updated
				) VALUES (
					@m_id,
					@m_category,
					@m_value,
					NOW()
				) ON CONFLICT (metric_id) DO UPDATE SET 
					metric_category = EXCLUDED.metric_category,
					metric_value = EXCLUDED.metric_value,
					metric_last_updated = NOW()";
		}

		public async Task<int> WriteAsync ( IEnumerable<AppMetric> appMetrics )
		{
			int affectedRows = 0;

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			using ( NpgsqlCommand upsertCmd = new NpgsqlCommand( mMetricsUpsertSql, conn, tx ) )
			{
				try
				{
					NpgsqlParameter pMetricId = upsertCmd.Parameters.Add( "m_id",
						NpgsqlDbType.Varchar );
					NpgsqlParameter pMetricCategory = upsertCmd.Parameters.Add( "metric_category",
						NpgsqlDbType.Varchar );
					NpgsqlParameter pMetricValue = upsertCmd.Parameters.Add( "m_value",
						NpgsqlDbType.Bigint );

					await upsertCmd.PrepareAsync();

					foreach ( AppMetric m in appMetrics )
					{
						pMetricId.Value = m.Id.ValueId;
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
