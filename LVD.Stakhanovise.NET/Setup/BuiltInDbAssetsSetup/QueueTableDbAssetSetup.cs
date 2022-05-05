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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class QueueTableDbAssetSetup : ISetupDbAsset
	{
		public const string TaskIdColumnName = "task_id";

		public const string TaskLocKHandleIdColumnName = "task_lock_handle_id";

		public const string TaskTypeColumnName = "task_type";

		public const string TaskSourceColumnName = "task_source";

		public const string TaskPayloadColumnName = "task_payload";

		public const string TaskPriorityColumnName = "task_priority";

		public const string TaskPostedAtColumnName = "task_posted_at_ts";

		public const string TaskLockedUntilColumnName = "task_locked_until_ts";

		public const string LockHandleIdSequenceNameFormat = "{0}_task_lock_handle_id_seq";

		public const string FilterIndexNameFormat = "idx_{0}_filter_index";

		public const string SortIndexNameFormat = "idx_{0}_sort_index";

		private bool mCreateSortIndex;

		private bool mCreateFilterIndex;

		public QueueTableDbAssetSetup ()
		{
			mCreateSortIndex = true;
			mCreateFilterIndex = true;
		}

		public QueueTableDbAssetSetup ( bool createSortIndex, bool createFilterIndex )
		{
			mCreateSortIndex = createSortIndex;
			mCreateFilterIndex = createFilterIndex;
		}

		private string GetLockHandleIdSequenceCreationScript ( QueuedTaskMapping mapping )
		{
			string lockHandleIdSeqName = GetLockHandleIdSequenceName( mapping );
			return $@"CREATE SEQUENCE IF NOT EXISTS public.{lockHandleIdSeqName}
				INCREMENT 1
				START 1
				MINVALUE 1
				MAXVALUE 9223372036854775807
				CACHE 1;";
		}

		private string GetLockHandleIdSequenceName ( QueuedTaskMapping mapping )
		{
			return string.Format( LockHandleIdSequenceNameFormat, mapping.QueueTableName );
		}

		private string GetDbTableCreationScript ( QueuedTaskMapping mapping )
		{
			string lockHandleIdSeqName = GetLockHandleIdSequenceName( mapping );
			return $@"CREATE TABLE IF NOT EXISTS public.{mapping.QueueTableName}
				(
					{TaskIdColumnName} uuid NOT NULL,
					{TaskLocKHandleIdColumnName} bigint NOT NULL DEFAULT nextval('{lockHandleIdSeqName}'::regclass),
					{TaskTypeColumnName} character varying(250) NOT NULL,
					{TaskSourceColumnName} character varying(250) NOT NULL,
					{TaskPayloadColumnName} text,
					{TaskPriorityColumnName} integer NOT NULL,
					{TaskPostedAtColumnName} timestamp with time zone NOT NULL DEFAULT now (),
					{TaskLockedUntilColumnName} timestamp with time zone NOT NULL,
					CONSTRAINT pk_{mapping.QueueTableName}_task_id PRIMARY KEY ( task_id),
					CONSTRAINT unq_{mapping.QueueTableName}_task_lock_handle_id UNIQUE( task_lock_handle_id )
				);";
		}

		private string GetFilterIndexCreationScript ( QueuedTaskMapping mapping )
		{
			string indexName = string.Format( FilterIndexNameFormat, mapping.QueueTableName );
			return $@"CREATE INDEX IF NOT EXISTS {indexName}
				ON public.{mapping.QueueTableName} USING btree
				(task_type ASC NULLS LAST, 
					task_locked_until_ts ASC NULLS LAST);";
		}

		private string GetSortIndexCreationScript ( QueuedTaskMapping mapping )
		{
			string indexName = string.Format( SortIndexNameFormat, mapping.QueueTableName );
			return $@"CREATE INDEX IF NOT EXISTS {indexName}
				ON public.{mapping.QueueTableName} USING btree
				(task_priority ASC NULLS LAST, 
					task_locked_until_ts ASC NULLS LAST, 
					task_lock_handle_id ASC NULLS LAST);";
		}

		public async Task SetupDbAssetAsync ( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdLockHandleIdSeq = new NpgsqlCommand( GetLockHandleIdSequenceCreationScript( mapping ), 
						conn ) )
					await cmdLockHandleIdSeq.ExecuteNonQueryAsync();

				using ( NpgsqlCommand cmdTable = new NpgsqlCommand( GetDbTableCreationScript( mapping ), 
						conn ) )
					await cmdTable.ExecuteNonQueryAsync();

				if ( mCreateSortIndex )
				{
					using ( NpgsqlCommand cmdCreateSortIndex = new NpgsqlCommand( GetSortIndexCreationScript( mapping ), 
							conn ) )
						await cmdCreateSortIndex.ExecuteNonQueryAsync();
				}

				if ( mCreateFilterIndex )
				{
					using ( NpgsqlCommand cmdCreateFilterIndex = new NpgsqlCommand( GetFilterIndexCreationScript( mapping ), 
							conn ) )
						await cmdCreateFilterIndex.ExecuteNonQueryAsync();
				}
			}
		}
	}
}
