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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class ExecutionTimeStatsTableDbAssetSetup : ISetupDbAsset
	{
		public const string PayloadTypeColumnName = "et_payload_type";

		public const string OwnerProcessIdColumnName = "et_owner_process_id";

		public const string NExecutionCyclesColumnName = "et_n_execution_cycles";

		public const string LastExecutionTimeColumnName = "et_last_execution_time";

		public const string AverageExecutionTimeColumnName = "et_avg_execution_time";

		public const string FastestExecutionTimeColumnName = "et_fastest_execution_time";

		public const string LongestExecutionTimeColumnName = "et_longest_execution_time";

		public const string TotalExecutionTimeColumnName = "et_total_execution_time";

		private string GetDbTableCreationScript( QueuedTaskMapping mapping )
		{
			return $@"CREATE TABLE IF NOT EXISTS public.{mapping.ExecutionTimeStatsTableName}
				(
					{PayloadTypeColumnName} character varying(255) NOT NULL,
					{OwnerProcessIdColumnName} character varying(255) NOT NULL,
					{NExecutionCyclesColumnName} bigint NOT NULL,
					{LastExecutionTimeColumnName} bigint NOT NULL,
					{AverageExecutionTimeColumnName} bigint NOT NULL,
					{FastestExecutionTimeColumnName} bigint NOT NULL,
					{LongestExecutionTimeColumnName} bigint NOT NULL,
					{TotalExecutionTimeColumnName} bigint NOT NULL,
					CONSTRAINT pk_{mapping.ExecutionTimeStatsTableName} PRIMARY KEY ({PayloadTypeColumnName}, {OwnerProcessIdColumnName})
				);";
		}

		public async Task SetupDbAssetAsync( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdTable = new NpgsqlCommand( GetDbTableCreationScript( mapping ), conn ) )
					await cmdTable.ExecuteNonQueryAsync();
			}
		}
	}
}
