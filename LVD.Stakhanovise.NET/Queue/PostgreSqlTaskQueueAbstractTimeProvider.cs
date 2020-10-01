using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueAbstractTimeProvider : ITaskQueueAbstractTimeProvider
	{
		private PostgreSqlTaskQueueAbstractTimeProviderOptions mProviderOptions;

		public PostgreSqlTaskQueueAbstractTimeProvider ( PostgreSqlTaskQueueAbstractTimeProviderOptions providerOptions )
		{
			mProviderOptions = providerOptions
				?? throw new ArgumentNullException( nameof( providerOptions ) );
		}
		private async Task<NpgsqlConnection> OpenConnectionAsync ( CancellationToken cancellationToken )
		{
			return await mProviderOptions
				.ConnectionOptions
				.TryOpenConnectionAsync( cancellationToken );
		}

		public async Task<long> ComputeAbsoluteTimeTicksAsync ( long timeTicksToAdd )
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
			{
				string computeSql = "SELECT sk_compute_absolute_time_ticks(@s_time_id, @s_time_ticks_to_add)";
				using ( NpgsqlCommand computeCmd = new NpgsqlCommand( computeSql, conn ) )
				{
					computeCmd.Parameters.AddWithValue( "s_time_id", NpgsqlDbType.Uuid,
						mProviderOptions.TimeId );
					computeCmd.Parameters.AddWithValue( "s_time_ticks_to_add", NpgsqlDbType.Bigint,
						timeTicksToAdd );

					await computeCmd.PrepareAsync();
					object result = await computeCmd.ExecuteScalarAsync();

					return result is long
						? ( long )result
						: 0;
				}
			}
		}

		public async Task<AbstractTimestamp> GetCurrentTimeAsync ()
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
			{
				AbstractTimestamp now = null;
				string selectSql = "SELECT * from sk_time_t WHERE t_id = @s_time_id";

				using ( NpgsqlCommand selectCmd = new NpgsqlCommand( selectSql, conn ) )
				{
					selectCmd.Parameters.AddWithValue( "s_time_id", NpgsqlDbType.Uuid,
						mProviderOptions.TimeId );

					await selectCmd.PrepareAsync();
					using ( NpgsqlDataReader selectRdr = await selectCmd.ExecuteReaderAsync() )
					{
						if ( await selectRdr.ReadAsync() )
						{
							long totalTicks = selectRdr.GetInt64( selectRdr.GetOrdinal(
								"t_total_ticks"
							) );

							long totalTicksCost = selectRdr.GetInt64( selectRdr.GetOrdinal(
								"t_total_ticks_cost"
							) );

							now = new AbstractTimestamp( totalTicks, 
								totalTicksCost );
						}
					}
				}

				return now;
			}
		}
	}
}
