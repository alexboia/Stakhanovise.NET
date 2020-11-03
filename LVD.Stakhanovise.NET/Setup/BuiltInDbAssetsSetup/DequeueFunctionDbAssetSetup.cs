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
	public class DequeueFunctionDbAssetSetup : ISetupDbAsset
	{
		private string GetDequeueFunctionCreationScript ( QueuedTaskMapping mapping )
		{
			return $@"CREATE OR REPLACE FUNCTION public.{mapping.DequeueFunctionName}(
					select_types character varying[],
					exclude_ids uuid[],
					ref_now timestamp with time zone)
				RETURNS TABLE(task_id uuid, 
					task_lock_handle_id bigint, 
					task_type character varying, 
					task_source character varying, 
					task_payload text, 
					task_priority integer, 
					task_posted_at_ts timestamp with time zone, 
					task_locked_until_ts timestamp with time zone) 
				LANGUAGE 'plpgsql'

			AS $BODY$
				DECLARE
					n_select_types integer = CARDINALITY(select_types);
	
				BEGIN
					RETURN QUERY 
					WITH sk_dequeued_task AS
						(DELETE FROM {mapping.QueueTableName} td WHERE td.task_id = (
							SELECT t0.task_id
									FROM {mapping.QueueTableName} t0 
									WHERE (t0.task_type = ANY(select_types) OR n_select_types = 0)
										AND t0.task_id <> ALL(exclude_ids)
										AND t0.task_locked_until_ts < ref_now
									ORDER BY t0.task_priority ASC,
										t0.task_locked_until_ts ASC,
										t0.task_lock_handle_id ASC
									LIMIT 1
									FOR UPDATE SKIP LOCKED
						) RETURNING *) SELECT sdt.* FROM sk_dequeued_task sdt;
				END;
			$BODY$;";
		}

		public async Task SetupDbAssetAsync ( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdDequeueFunction = new NpgsqlCommand( GetDequeueFunctionCreationScript( mapping ), conn ) )
					await cmdDequeueFunction.ExecuteNonQueryAsync();
			}
		}
	}
}
