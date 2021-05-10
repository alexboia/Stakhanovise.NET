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
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using LVD.Stakhanovise.NET.Helpers;

namespace LVD.Stakhanovise.NET.Setup
{
	public class AppMetricsTableDbAssetSetup : ISetupDbAsset
	{
		public const string MetricIdColumnName = "metric_id";

		public const string MetricCategoryColumnName = "metric_category";

		public const string MetricValueColumnName = "metric_value";

		public const string MetricLastUpdatedColumnName = "metric_last_updated";

		public const string MetricCategoryIndexFormat = "idx_{0}_metric_category";

		private string GetDbTableCreationScript ( QueuedTaskMapping mapping )
		{
			return $@"CREATE TABLE IF NOT EXISTS public.{mapping.MetricsTableName}
				(
					{MetricIdColumnName} character varying(250) NOT NULL,
					{MetricCategoryColumnName} character varying( 150 ) NOT NULL,
					{MetricValueColumnName} bigint NOT NULL DEFAULT 0,
					{MetricLastUpdatedColumnName} timestamp with time zone NOT NULL DEFAULT now(),
					CONSTRAINT {mapping.MetricsTableName}_pkey PRIMARY KEY ( metric_id)
				);";
		}

		private string GetMetricCategoryIndexCreationScript ( QueuedTaskMapping mapping )
		{
			string indexName = string.Format( MetricCategoryIndexFormat, mapping.MetricsTableName );
			return $@"CREATE INDEX IF NOT EXISTS {indexName} 
					ON public.{mapping.MetricsTableName} USING btree 
					(metric_category ASC NULLS LAST);";
		}

		public async Task SetupDbAssetAsync ( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdTable = new NpgsqlCommand( GetDbTableCreationScript( mapping ), 
						conn ) )
					await cmdTable.ExecuteNonQueryAsync();

				using ( NpgsqlCommand cmdTableIndex = new NpgsqlCommand( GetMetricCategoryIndexCreationScript( mapping ), 
						conn ) )
					await cmdTableIndex.ExecuteNonQueryAsync();
			}
		}
	}
}
