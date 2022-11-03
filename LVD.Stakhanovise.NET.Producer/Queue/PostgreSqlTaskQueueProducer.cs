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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueProducer : ITaskQueueProducer
	{
		private const int ImmediatePastSecondsInterval = 1;

		private readonly TaskQueueOptions mOptions;

		private readonly string mInsertSql;

		private readonly string mAddOrUpdateResultSql;

		private readonly ITimestampProvider mTimestampProvider;

		public PostgreSqlTaskQueueProducer( TaskQueueOptions options, ITimestampProvider timestampProvider )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );
			if ( timestampProvider == null )
				throw new ArgumentNullException( nameof( timestampProvider ) );

			mOptions = options;
			mTimestampProvider = timestampProvider;

			mInsertSql = GetInsertSql( mOptions.Mapping );
			mAddOrUpdateResultSql = GetInsertOrUpdateResultSql( mOptions.Mapping );
		}

		private string GetInsertSql( QueuedTaskMapping mapping )
		{
			return $@"INSERT INTO {mapping.QueueTableName} (
					task_id, task_payload, task_type, task_source, task_priority, task_posted_at_ts, task_locked_until_ts
				) VALUES (
					@t_id, @t_payload, @t_type, @t_source, @t_priority, @t_posted_at_ts, @t_locked_until_ts
				) RETURNING task_lock_handle_id";
		}

		private string GetInsertOrUpdateResultSql( QueuedTaskMapping mapping )
		{
			return $@"INSERT INTO {mapping.ResultsQueueTableName} (
					task_id, task_type, task_source, task_payload, task_status, task_priority, task_posted_at_ts
				) VALUES (
					@t_id, @t_type, @t_source, @t_payload, @t_status, @t_priority, @t_posted_at_ts
				) ON CONFLICT (task_id) DO UPDATE SET 
					task_status = EXCLUDED.task_status,
					task_priority = EXCLUDED.task_priority,
					task_source = EXCLUDED.task_source,
					task_posted_at_ts = EXCLUDED.task_posted_at_ts";
		}

		public async Task<IQueuedTask> EnqueueAsync<TPayload>( TPayload payload,
			string source,
			int priority )
		{
			if ( EqualityComparer<TPayload>.Default.Equals( payload, default( TPayload ) ) )
				throw new ArgumentNullException( nameof( payload ) );

			if ( string.IsNullOrEmpty( source ) )
				throw new ArgumentNullException( nameof( source ) );

			if ( priority < 0 )
				throw new ArgumentOutOfRangeException( nameof( priority ), "Priority must be greater than or equal to 0" );

			return await EnqueueAsync( new QueuedTaskProduceInfo()
			{
				Payload = payload,
				Type = DeterminePayloadTypeFullName<TPayload>(),
				Priority = priority,
				Source = source,
				LockedUntilTs = GenerateImmediatePastLockedTimestamp(),
				Status = QueuedTaskStatus.Unprocessed
			} );
		}

		private string DeterminePayloadTypeFullName<TPayload>()
		{
			return typeof( TPayload )
				.FullName;
		}

		private DateTimeOffset GenerateImmediatePastLockedTimestamp()
		{
			return mTimestampProvider
				.GetNow()
				.AddSeconds( -ImmediatePastSecondsInterval );
		}

		public async Task<IQueuedTask> EnqueueAsync( QueuedTaskProduceInfo produceInfo )
		{
			if ( produceInfo == null )
				throw new ArgumentNullException( nameof( produceInfo ) );

			if ( produceInfo.Priority < 0 )
				throw new ArgumentOutOfRangeException( nameof( produceInfo ), "Priority must be greater than or equal to 0" );

			using ( NpgsqlConnection conn = await TryOpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			{
				IQueuedTask queuedTask = await TryPostTaskAsync( produceInfo, conn, tx );
				
				await TryInitOrUpdateResultAsync( queuedTask, produceInfo, conn, tx );
				await NotifyNewTaskPostedAsync( queuedTask, conn, tx );			
				tx.Commit();

				return queuedTask;
			}
		}

		private async Task<NpgsqlConnection> TryOpenConnectionAsync()
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
		}

		private QueuedTask CreateNewTaskFromInfo( QueuedTaskProduceInfo queuedTaskInfo )
		{
			return new QueuedTask( queuedTaskInfo.CreateNewTask( mTimestampProvider ) );
		}

		private async Task<IQueuedTask> TryPostTaskAsync( QueuedTaskProduceInfo produceInfo,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{

			QueuedTask savedTask = CreateNewTaskFromInfo( produceInfo );

			using ( NpgsqlCommand insertCmd = new NpgsqlCommand( mInsertSql, conn, tx ) )
			{
				insertCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					savedTask.Id );
				insertCmd.Parameters.AddWithValue( "t_payload",
					NpgsqlDbType.Text,
					ConvertPayloadToJson( savedTask.Payload ) );
				insertCmd.Parameters.AddWithValue( "t_type",
					NpgsqlDbType.Varchar,
					savedTask.Type );
				insertCmd.Parameters.AddWithValue( "t_source",
					NpgsqlDbType.Varchar,
					savedTask.Source );
				insertCmd.Parameters.AddWithValue( "t_priority",
					NpgsqlDbType.Integer,
					savedTask.Priority );
				insertCmd.Parameters.AddWithValue( "t_locked_until_ts",
					NpgsqlDbType.TimestampTz,
					savedTask.LockedUntilTs );
				insertCmd.Parameters.AddWithValue( "t_posted_at_ts",
					NpgsqlDbType.TimestampTz,
					savedTask.PostedAtTs );

				savedTask.LockHandleId = ( long ) await insertCmd
					.ExecuteScalarAsync();
			}

			return savedTask;
		}

		private string ConvertPayloadToJson( object payload )
		{
			return payload.ToJson( mOptions
					.SerializerOptions
					.OnConfigureSerializerSettings,
				includeTypeInformation: true );
		}

		private async Task TryInitOrUpdateResultAsync( IQueuedTask queuedTask,
			QueuedTaskProduceInfo produceInfo,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			using ( NpgsqlCommand addOrUpdateResultCmd = new NpgsqlCommand( mAddOrUpdateResultSql, conn, tx ) )
			{
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					queuedTask.Id );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_type",
					NpgsqlDbType.Varchar,
					queuedTask.Type );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_source",
					NpgsqlDbType.Varchar,
					queuedTask.Source );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_payload",
					NpgsqlDbType.Text,
					ConvertPayloadToJson( queuedTask.Payload ) );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_status",
					NpgsqlDbType.Integer,
					( int ) produceInfo.Status );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_priority",
					NpgsqlDbType.Integer,
					queuedTask.Priority );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_posted_at_ts",
					NpgsqlDbType.TimestampTz,
					queuedTask.PostedAtTs );

				await addOrUpdateResultCmd.PrepareAsync();
				await addOrUpdateResultCmd.ExecuteNonQueryAsync();
			}
		}

		private async Task NotifyNewTaskPostedAsync( IQueuedTask queuedTask, NpgsqlConnection conn, NpgsqlTransaction tx )
		{
			await conn.NotifyAsync( mOptions.Mapping.NewTaskNotificationChannelName,
				tx );
		}

		public ITimestampProvider TimestampProvider
			=> mTimestampProvider;
	}
}
