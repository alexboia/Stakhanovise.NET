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
			long absoluteTimeTicks = 0;
			string computeSql = "SELECT sk_compute_absolute_time_ticks(@s_time_id, @s_time_ticks_to_add)";

			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
			using ( NpgsqlCommand computeCmd = new NpgsqlCommand( computeSql, conn ) )
			{
				computeCmd.Parameters.AddWithValue( "s_time_id", NpgsqlDbType.Uuid,
					mProviderOptions.TimeId );
				computeCmd.Parameters.AddWithValue( "s_time_ticks_to_add", NpgsqlDbType.Bigint,
					timeTicksToAdd );

				await computeCmd.PrepareAsync();
				object result = await computeCmd.ExecuteScalarAsync();

				absoluteTimeTicks = result is long
					? ( long )result
					: 0;

				await conn.CloseAsync();
			}

			return absoluteTimeTicks;
		}

		public async Task<AbstractTimestamp> GetCurrentTimeAsync ()
		{
			AbstractTimestamp now = null;
			string selectSql = "SELECT * from sk_time_t WHERE t_id = @s_time_id";

			using ( NpgsqlConnection conn = await OpenConnectionAsync( CancellationToken.None ) )
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

				await conn.CloseAsync();
			}

			return now;
		}
	}
}
