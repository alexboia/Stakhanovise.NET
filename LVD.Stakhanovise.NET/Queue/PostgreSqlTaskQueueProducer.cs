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
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueProducer : ITaskQueueProducer
	{
		private TaskQueueOptions mOptions;

		public PostgreSqlTaskQueueProducer ( TaskQueueOptions options )
		{
			//TODO: use task queue producer options
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			mOptions = options;
		}

		private async Task<NpgsqlConnection> TryOpenConnectionAsync ()
		{
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
		}

		public async Task<IQueuedTask> EnqueueAsync<TPayload> ( TPayload payload,
			AbstractTimestamp now,
			string source,
			int priority )
		{
			QueuedTask queuedTask = null;

			if ( EqualityComparer<TPayload>.Default.Equals( payload, default( TPayload ) ) )
				throw new ArgumentNullException( nameof( payload ) );

			if ( now == null )
				throw new ArgumentNullException( nameof( now ) );

			if ( string.IsNullOrEmpty( source ) )
				throw new ArgumentNullException( nameof( source ) );

			if ( priority < 0 )
				throw new ArgumentOutOfRangeException( nameof( priority ), "Priority must be greater than or equal to 0" );

			queuedTask = new QueuedTask();
			queuedTask.Id = Guid.NewGuid();
			queuedTask.PostedAt = now.Ticks;
			queuedTask.Payload = payload;
			queuedTask.Type = typeof( TPayload ).FullName;
			queuedTask.Source = source;
			queuedTask.Status = QueuedTaskStatus.Unprocessed;
			queuedTask.Priority = priority;
			queuedTask.PostedAtTs = DateTimeOffset.UtcNow;
			queuedTask.RepostedAtTs = DateTimeOffset.UtcNow;
			queuedTask.ErrorCount = 0;

			using ( NpgsqlConnection conn = await TryOpenConnectionAsync() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			{
				string insertSql = $@"INSERT INTO {mOptions.Mapping.TableName} (
						{mOptions.Mapping.IdColumnName},
						{mOptions.Mapping.PayloadColumnName},
						{mOptions.Mapping.TypeColumnName},
						{mOptions.Mapping.SourceColumnName},
						{mOptions.Mapping.PriorityColumnName},
						{mOptions.Mapping.PostedAtColumnName},
						{mOptions.Mapping.PostedAtTsColumnName},
						{mOptions.Mapping.RepostedAtTsColumnName},
						{mOptions.Mapping.ErrorCountColumnName},
						{mOptions.Mapping.LastErrorIsRecoverableColumnName},
						{mOptions.Mapping.StatusColumnName}
					) VALUES (
						@t_id,
						@t_payload,
						@t_type,
						@t_source,
						@t_priority,
						@t_posted_at,
						@t_posted_at_ts,
						@t_reposted_at_ts,
						@t_error_count,
						@t_last_error_is_recoverable,
						@t_status
					)";

				using ( NpgsqlCommand insertCmd = new NpgsqlCommand( insertSql, conn, tx ) )
				{
					insertCmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
						queuedTask.Id );
					insertCmd.Parameters.AddWithValue( "t_payload", NpgsqlDbType.Text,
						queuedTask.Payload.ToJson( includeTypeInformation: true ) );
					insertCmd.Parameters.AddWithValue( "t_type", NpgsqlDbType.Varchar,
						queuedTask.Type );
					insertCmd.Parameters.AddWithValue( "t_source", NpgsqlDbType.Varchar,
						queuedTask.Source );
					insertCmd.Parameters.AddWithValue( "t_priority", NpgsqlDbType.Integer,
						queuedTask.Priority );
					insertCmd.Parameters.AddWithValue( "t_posted_at", NpgsqlDbType.Bigint,
						queuedTask.PostedAt );
					insertCmd.Parameters.AddWithValue( "t_posted_at_ts", NpgsqlDbType.TimestampTz,
						queuedTask.PostedAtTs );
					insertCmd.Parameters.AddWithValue( "t_reposted_at_ts", NpgsqlDbType.TimestampTz,
						queuedTask.RepostedAtTs );
					insertCmd.Parameters.AddWithValue( "t_error_count", NpgsqlDbType.Integer,
						queuedTask.ErrorCount );
					insertCmd.Parameters.AddWithValue( "t_last_error_is_recoverable", NpgsqlDbType.Boolean,
						queuedTask.LastErrorIsRecoverable );
					insertCmd.Parameters.AddWithValue( "t_status", NpgsqlDbType.Integer,
						( int )queuedTask.Status );

					await insertCmd.ExecuteNonQueryAsync();
					await conn.NotifyAsync( mOptions.Mapping.NewTaskNotificaionChannelName,
						tx );
				}

				tx.Commit();
			}

			return queuedTask;
		}
	}
}
