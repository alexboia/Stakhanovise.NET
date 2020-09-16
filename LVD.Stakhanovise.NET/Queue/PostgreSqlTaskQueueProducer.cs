﻿using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
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
		private bool mIsDisposed = false;

		private string mQueueConnectionString;

		private TaskQueueOptions mOptions;

		public PostgreSqlTaskQueueProducer ( TaskQueueOptions options )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			mOptions = options;
			mQueueConnectionString = options.ConnectionString
				.DeriveQueueConnectionString( options );
		}

		private async Task<NpgsqlConnection> TryOpenConnectionAsync ()
		{
			return await mQueueConnectionString.TryOpenConnectionAsync( mOptions.ConnectionRetryCount, 
				mOptions.ConnectionRetryDelay );
		}

		public async Task<IQueuedTask> EnqueueAsync<TPayload> ( TPayload payload, string source, int priority )
		{
			QueuedTask queuedTask = null;

			if ( EqualityComparer<TPayload>.Default.Equals( payload, default( TPayload ) ) )
				throw new ArgumentNullException( nameof( payload ) );

			if ( string.IsNullOrEmpty( source ) )
				throw new ArgumentNullException( nameof( source ) );

			if ( priority < 0 )
				throw new ArgumentOutOfRangeException( nameof( priority ), "Priority must be greater than or equal to 0" );

			queuedTask = new QueuedTask();
			queuedTask.Id = Guid.NewGuid();
			queuedTask.Payload = payload;
			queuedTask.Type = typeof( TPayload ).FullName;
			queuedTask.Source = source;
			queuedTask.Status = QueuedTaskStatus.Unprocessed;
			queuedTask.Priority = priority;
			queuedTask.PostedAt = DateTimeOffset.UtcNow;
			queuedTask.RepostedAt = DateTimeOffset.UtcNow;
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
						{mOptions.Mapping.RepostedAtColumnName},
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
						@t_reposted_at,
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
					insertCmd.Parameters.AddWithValue( "t_posted_at", NpgsqlDbType.TimestampTz,
						queuedTask.PostedAt );
					insertCmd.Parameters.AddWithValue( "t_reposted_at", NpgsqlDbType.TimestampTz,
						queuedTask.RepostedAt );
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
