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
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueConsumer : ITaskQueueConsumer, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsDisposed;

		private TaskQueueConsumerOptions mOptions;

		private string mSignalingConnectionString;

		private string mQueueConnectionString;

		private PostgreSqlTaskQueueNotificationListener mNotificationListener;

		private ITaskQueueAbstractTimeProvider mTimeProvider;

		public PostgreSqlTaskQueueConsumer ( TaskQueueConsumerOptions options, ITaskQueueAbstractTimeProvider timeProvider )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			if ( timeProvider == null )
				throw new ArgumentNullException( nameof( timeProvider ) );

			mOptions = options;
			mTimeProvider = timeProvider;

			mSignalingConnectionString = options.ConnectionOptions
				.ConnectionString
				.DeriveSignalingConnectionString( options );
			mQueueConnectionString = options.ConnectionOptions
				.ConnectionString
				.DeriveQueueConsumerConnectionString( options );

			mNotificationListener = new PostgreSqlTaskQueueNotificationListener( mSignalingConnectionString,
				options.Mapping.NewTaskNotificaionChannelName );

			mNotificationListener.ListenerConnectionRestored +=
				HandleListenerConnectionRestored;
			mNotificationListener.NewTaskPosted +=
				HandleNewTaskUpdateReceived;
			mNotificationListener.ListenerTimedOutWhileWaiting +=
				HandleListenerTimedOut;
		}

		private async Task<NpgsqlConnection> OpenSignalingConnectionAsync ()
		{
			return await mSignalingConnectionString.TryOpenConnectionAsync(
				mOptions.ConnectionOptions
					.ConnectionRetryCount,
				mOptions.ConnectionOptions
					.ConnectionRetryDelayMilliseconds
			);
		}

		private async Task<NpgsqlConnection> OpenQueueConnectionAsync ()
		{
			return await mQueueConnectionString.TryOpenConnectionAsync(
				mOptions.ConnectionOptions
					.ConnectionRetryCount,
				mOptions.ConnectionOptions
					.ConnectionRetryDelayMilliseconds
			);
		}

		private void NotifyClearForDequeue ( ClearForDequeReason reason )
		{
			EventHandler<ClearForDequeueEventArgs> eventHandler = ClearForDequeue;
			if ( eventHandler != null )
				eventHandler( this, new ClearForDequeueEventArgs( reason ) );
		}

		private void HandleNewTaskUpdateReceived ( object sender, NewTaskPostedEventArgs e )
		{
			NotifyClearForDequeue( ClearForDequeReason
				.NewTaskPostedNotificationReceived );
		}

		private void HandleListenerConnectionRestored ( object sender, ListenerConnectionRestoredEventArgs e )
		{
			NotifyClearForDequeue( ClearForDequeReason
				.NewTaskListenerConnectionStateChange );
		}

		private void HandleListenerTimedOut ( object sender, ListenerTimedOutEventArgs e )
		{
			NotifyClearForDequeue( ClearForDequeReason
				.ListenerTimedOut );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueConsumer ),
					"Cannot reuse a disposed postgre sql task queue consumer" );
		}

		public async Task<IQueuedTaskToken> DequeueAsync ( params string[] selectTaskTypes )
		{
			NpgsqlConnection conn = null;
			QueuedTask dequeuedTask = null;
			QueuedTaskResult dequeuedTaskResult = null;
			IQueuedTaskToken dequeuedTaskToken = null;

			CheckNotDisposedOrThrow();

			Guid[] excludeLockedTaskIds =
				new Guid[ 0 ];

			try
			{
				mLogger.DebugFormat( "Begin dequeue task. Looking for types: {0}.",
					string.Join<string>( ",", selectTaskTypes ) );

				AbstractTimestamp now = await mTimeProvider
					.GetCurrentTimeAsync();

				//Time connection establishment - this directly influences 
				//	how much time the lock is being held
				MonotonicTimestamp startConnect = MonotonicTimestamp
					.Now();

				conn = await OpenQueueConnectionAsync();
				if ( conn == null )
					return null;

				MonotonicTimestamp endConnect = MonotonicTimestamp
					.Now();

				using ( NpgsqlTransaction dequeueTx = conn.BeginTransaction() )
				using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand() )
				{
					dequeueCmd.Connection = conn;
					dequeueCmd.Transaction = dequeueTx;
					dequeueCmd.CommandText = $"SELECT tq.* FROM { mOptions.Mapping.DequeueFunctionName }(@types, @excluded, @now) tq";

					dequeueCmd.Parameters.AddWithValue( "types",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Varchar,
						value: selectTaskTypes );

					dequeueCmd.Parameters.AddWithValue( "excluded",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Uuid,
						value: excludeLockedTaskIds );

					dequeueCmd.Parameters.AddWithValue( "now",
						parameterType: NpgsqlDbType.Bigint,
						value: now.Ticks );

					await dequeueCmd.PrepareAsync();
					using ( NpgsqlDataReader taskRdr = await dequeueCmd.ExecuteReaderAsync() )
					{
						dequeuedTask = await taskRdr.ReadAsync()
							? await taskRdr.ReadQueuedTaskAsync()
							: null;

						//If a new task has been found, save it 
						//	along with the database connection 
						//	that holds the lock
						if ( dequeuedTask != null )
						{
							mLogger.DebugFormat( "Found with id = {0} in database queue. Attempting to acquire...",
								dequeuedTask.Id );

							using ( NpgsqlCommand removeCmd = new NpgsqlCommand() )
							{
								removeCmd.Connection = conn;
								removeCmd.Transaction = dequeueTx;
								removeCmd.CommandText = $@"DELETE FROM {mOptions.Mapping.QueueTableName} WHERE task_id = @t_id";

								removeCmd.Parameters.AddWithValue( "t_id",
									NpgsqlDbType.Uuid,
									dequeuedTask.Id );

								await removeCmd.PrepareAsync();
								if ( await removeCmd.ExecuteNonQueryAsync() == 1 )
								{
									mLogger.Debug( "Task successfully acquired. Attempting to initialize result..." );

									using ( NpgsqlCommand addOrUpdateResultCmd = new NpgsqlCommand() )
									{
										addOrUpdateResultCmd.Connection = conn;
										addOrUpdateResultCmd.Transaction = dequeueTx;
										addOrUpdateResultCmd.CommandText = $@"INSERT INTO {mOptions.Mapping.ResultsTableName} (
												task_id,
												task_type,
												task_source,
												task_payload,
												task_status,
												task_priority,
												task_posted_at,
												task_posted_at_ts,
												task_first_processing_attempted_at
											) VALUES (
												@t_id,
												@t_type,
												@t_source,
												@t_payload,
												@t_status,
												@t_priority,
												@t_posted_at,
												@t_posted_at_ts,
												NOW()
											) ON CONFLICT (task_id) DO UPDATE 
												task_status = EXCLUDED.task_status,
												task_posted_at = EXCLUDED.task_posted_at,
												task_posted_at_ts = EXCLUDED.task_posted_at_ts,
												task_last_processing_attempted_at = NOW() 
											RETURNING *";

										addOrUpdateResultCmd.Parameters.AddWithValue( "t_id",
											NpgsqlDbType.Uuid,
											dequeuedTask.Id );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_type",
											NpgsqlDbType.Varchar,
											dequeuedTask.Type );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_source",
											NpgsqlDbType.Varchar,
											dequeuedTask.Source );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_payload",
											NpgsqlDbType.Text,
											dequeuedTask.Payload.ToJson( includeTypeInformation: true ) );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_status",
											NpgsqlDbType.Integer,
											( int )QueuedTaskStatus.Processing );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_priority",
											NpgsqlDbType.Integer,
											dequeuedTask.Priority );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_posted_at",
											NpgsqlDbType.Bigint,
											dequeuedTask.PostedAt );
										addOrUpdateResultCmd.Parameters.AddWithValue( "t_posted_at_ts",
											NpgsqlDbType.TimestampTz,
											dequeuedTask.PostedAtTs );

										await addOrUpdateResultCmd.PrepareAsync();
										using ( NpgsqlDataReader resultRdr = await addOrUpdateResultCmd
											.ExecuteReaderAsync() )
										{
											if ( await resultRdr.ReadAsync() )
												dequeuedTaskResult = await resultRdr.ReadQueuedTaskResultAsync();

											if ( dequeuedTaskResult != null )
											{
												await dequeueTx.CommitAsync();

												dequeuedTaskToken = new PostgreSqlQueuedTaskToken( dequeuedTask,
													dequeuedTaskResult,
													now );

												mLogger.Debug( "Successfully dequeued, acquired and initialized/updated task result." );
											}
											else
											{
												await dequeueTx.RollbackAsync();
												mLogger.Debug( "Failed to initialize or update task result. Will release lock..." );
											}
										}
									}
								}
								else
									mLogger.Debug( "Could not acquire task. Will release lock..." );
							}
						}
						else
							mLogger.Debug( "No task found to dequeue." );
					}
				}
			}
			finally
			{
				//If no task has been found, attempt to 
				//  release all locks held by this connection 
				//  and also close it
				if ( ( dequeuedTask == null || dequeuedTaskResult == null ) && conn != null )
					await conn.UnlockAllAsync();

				if ( conn != null )
				{
					await conn.CloseAsync();
					conn.Dispose();
				}
			}

			return dequeuedTaskToken;
		}

		public IQueuedTaskToken Dequeue ( params string[] supportedTypes )
		{
			Task<IQueuedTaskToken> asyncTask = DequeueAsync( supportedTypes );
			return asyncTask.Result;
		}

		public async Task StartReceivingNewTaskUpdatesAsync ()
		{
			CheckNotDisposedOrThrow();
			await mNotificationListener.StartAsync();
		}

		public async Task StopReceivingNewTaskUpdatesAsync ()
		{
			CheckNotDisposedOrThrow();
			await mNotificationListener.StopAsync();
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					ClearForDequeue = null;

					StopReceivingNewTaskUpdatesAsync()
						.Wait();

					mNotificationListener.ListenerConnectionRestored -=
						HandleListenerConnectionRestored;
					mNotificationListener.NewTaskPosted -=
						HandleNewTaskUpdateReceived;
					mNotificationListener.ListenerTimedOutWhileWaiting -=
						HandleListenerTimedOut;

					mNotificationListener.Dispose();
					mNotificationListener = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public bool IsReceivingNewTaskUpdates
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mNotificationListener.IsStarted;
			}
		}

		public ITaskQueueAbstractTimeProvider TimeProvider
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mTimeProvider;
			}
		}
	}
}
