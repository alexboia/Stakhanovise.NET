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
	public class QueueResultTableDbAssetSetup : ISetupDbAsset
	{
		public const string TaskIdColumnName = "task_id";

		public const string TaskTypeColumnName = "task_type";

		public const string TaskSourceColumnName = "task_source";

		public const string TaskPayloadColumnName = "task_payload";

		public const string TaskStatusColumnName = "task_status";

		public const string TaskPriorityColumnName = "task_priority";

		public const string TaskLastErrorColumnName = "task_last_error";

		public const string TaskErrorCountColumnName = "task_error_count";

		public const string TaskLastErrorIsRecoverableColumnName = "task_last_error_is_recoverable";

		public const string TaskProcessingTimeMillisecondsColumnName = "task_processing_time_milliseconds";

		public const string TaskPostedAtColumnName = "task_posted_at_ts";

		public const string TaskFirstProcessingAttemptedAtColumnName = "task_first_processing_attempted_at_ts";

		public const string TaskLastProcessingAttemptedAtColumnName = "task_last_processing_attempted_at_ts";

		public const string TaskProcessingFinalizedAtColumnName = "task_processing_finalized_at_ts";

		public const string TaskStatusIndexNameFormat = "idx_{0}_task_status";

		public const string TaskTypeIndexNameFormat = "idx_{0}_task_type";

		private bool mCreateTaskStatusIndex = false;

		private bool mCreateTaskTypeIndex = false;

		public QueueResultTableDbAssetSetup ()
		{
			mCreateTaskStatusIndex = true;
			mCreateTaskTypeIndex = true;
		}

		public QueueResultTableDbAssetSetup ( bool createTaskStatusIndex, bool createTaskTypeIndex )
		{
			mCreateTaskStatusIndex = createTaskStatusIndex;
			mCreateTaskTypeIndex = createTaskTypeIndex;
		}

		private string GetDbTableCreationScript ( QueuedTaskMapping mapping )
		{
			return $@"CREATE TABLE IF NOT EXISTS public.{mapping.ResultsQueueTableName}
				(
					{TaskIdColumnName} uuid NOT NULL,
					{TaskTypeColumnName} character varying(250) NOT NULL,
					{TaskSourceColumnName} character varying( 250 ) NOT NULL,
					{TaskPayloadColumnName} text ,
					{TaskStatusColumnName} integer NOT NULL,
					{TaskPriorityColumnName} integer NOT NULL,
					{TaskLastErrorColumnName} text ,
					{TaskErrorCountColumnName} integer NOT NULL DEFAULT 0,
					{TaskLastErrorIsRecoverableColumnName} boolean NOT NULL DEFAULT false,
					{TaskProcessingTimeMillisecondsColumnName} bigint NOT NULL DEFAULT 0,
					{TaskPostedAtColumnName} timestamp with time zone NOT NULL,
					{TaskFirstProcessingAttemptedAtColumnName} timestamp with time zone,
					{TaskLastProcessingAttemptedAtColumnName} timestamp with time zone,
					{TaskProcessingFinalizedAtColumnName} timestamp with time zone,
					CONSTRAINT pk_{mapping.ResultsQueueTableName}_task_id PRIMARY KEY ( task_id)
				);";
		}

		private string GetTaskStatusIndexCreationScript ( QueuedTaskMapping mapping )
		{
			string indexName = string.Format( TaskStatusIndexNameFormat, mapping.ResultsQueueTableName );
			return $@"CREATE INDEX IF NOT EXISTS {indexName}
					ON public.{mapping.ResultsQueueTableName} USING btree
					(task_status ASC NULLS LAST);";
		}

		private string GetTaskTypeIndexCreationScript ( QueuedTaskMapping mapping )
		{
			string indexName = string.Format( TaskTypeIndexNameFormat, mapping.ResultsQueueTableName );
			return $@"CREATE INDEX IF NOT EXISTS {indexName}
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
				using ( NpgsqlCommand cmdTable = new NpgsqlCommand( GetDbTableCreationScript( mapping ),
						conn ) )
					await cmdTable.ExecuteNonQueryAsync();

				if ( mCreateTaskStatusIndex )
				{
					using ( NpgsqlCommand cmdCreateTaskStatusIndex = new NpgsqlCommand( GetTaskStatusIndexCreationScript( mapping ),
							conn ) )
						await cmdCreateTaskStatusIndex.ExecuteNonQueryAsync();
				}

				if ( mCreateTaskTypeIndex )
				{
					using ( NpgsqlCommand cmdCreateTaskTypeIndex = new NpgsqlCommand( GetTaskTypeIndexCreationScript( mapping ),
							conn ) )
						await cmdCreateTaskTypeIndex.ExecuteNonQueryAsync();
				}
			}
		}
	}
}
