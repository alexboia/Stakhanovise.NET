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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class QueueResultTableDbAssetSetup : ISetupDbAsset
	{
		private string GetDbTableCreationScript ( QueuedTaskMapping mapping )
		{
			return $@"CREATE TABLE IF NOT EXISTS public.{mapping.ResultsQueueTableName}
				(
					task_id uuid NOT NULL,
					task_type character varying(250) NOT NULL,
					task_source character varying( 250 ) NOT NULL,
					task_payload text ,
					task_status integer NOT NULL,
					task_priority integer NOT NULL,
					task_last_error text ,
					task_error_count integer NOT NULL DEFAULT 0,
					task_last_error_is_recoverable boolean NOT NULL DEFAULT false,
					task_processing_time_milliseconds bigint NOT NULL DEFAULT 0,
					task_posted_at_ts timestamp with time zone NOT NULL,
					task_first_processing_attempted_at_ts timestamp with time zone,
					task_last_processing_attempted_at_ts timestamp with time zone,
					task_processing_finalized_at_ts timestamp with time zone,
					CONSTRAINT pk_{mapping.ResultsQueueTableName}_task_id PRIMARY KEY ( task_id)
				);";
		}

		private string GetDbTableIndexCreationScript ( QueuedTaskMapping mapping )
		{
			return $@"CREATE INDEX idx_{mapping.ResultsQueueTableName}_task_status
					ON public.{mapping.ResultsQueueTableName} USING btree
					(task_status ASC NULLS LAST);
				CREATE INDEX idx_{mapping.ResultsQueueTableName}_task_type
					ON public.{mapping.ResultsQueueTableName} USING btree
					(task_type ASC NULLS LAST);";
		}

		public async Task SetupDbAssetAsync ( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdTable = new NpgsqlCommand( GetDbTableCreationScript( mapping ), conn ) )
					await cmdTable.ExecuteNonQueryAsync();

				using ( NpgsqlCommand cmdTableIndex = new NpgsqlCommand( GetDbTableIndexCreationScript( mapping ), conn ) )
					await cmdTableIndex.ExecuteNonQueryAsync();
			}
		}
	}
}
