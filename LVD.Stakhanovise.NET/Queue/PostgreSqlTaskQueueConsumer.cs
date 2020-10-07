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

		private string mTaskDequeueSql;

		private string mTaskAcquireSql;

		private string mTaskResultUpdateSql;

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

			mTaskDequeueSql = GetTaskDequeueSql( options.Mapping );
			mTaskAcquireSql = GetTaskAcquireSql( options.Mapping );
			mTaskResultUpdateSql = GetTaskResultUpdateSql( options.Mapping );
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

		private string GetTaskDequeueSql ( QueuedTaskMapping mapping )
		{
			return $@"SELECT tq.* FROM { mapping.DequeueFunctionName }(@types, @excluded, @now) tq";
		}

		private string GetTaskAcquireSql ( QueuedTaskMapping mapping )
		{
			return $@"DELETE FROM {mapping.QueueTableName} 
				WHERE task_id = @t_id";
		}

		private string GetTaskResultUpdateSql ( QueuedTaskMapping mapping )
		{
			return $@"UPDATE {mapping.ResultsTableName} SET
					task_status = @t_status,
					task_first_processing_attempted_at_ts = COALESCE(task_first_processing_attempted_at_ts, NOW()),
					task_last_processing_attempted_at_ts = NOW() 
				WHERE task_id = @t_id
				RETURNING *";
		}

		public async Task<IQueuedTaskToken> DequeueAsync ( params string[] selectTaskTypes )
		{
			NpgsqlConnection conn = null;
			QueuedTask dequeuedTask = null;
			QueuedTaskResult dequeuedTaskResult = null;
			PostgreSqlQueuedTaskToken dequeuedTaskToken = null;

			CheckNotDisposedOrThrow();

			try
			{
				mLogger.DebugFormat( "Begin dequeue task. Looking for types: {0}.",
					string.Join<string>( ",", selectTaskTypes ) );

				AbstractTimestamp now = await mTimeProvider
					.GetCurrentTimeAsync();

				conn = await OpenQueueConnectionAsync();
				if ( conn == null )
					return null;

				using ( NpgsqlTransaction tx = conn.BeginTransaction() )
				{
					dequeuedTask = await TryDequeueTaskAsync( selectTaskTypes, now, conn, tx );
					if ( dequeuedTask != null && await TryAcquireTaskAsync( dequeuedTask, conn, tx ) )
					{
						dequeuedTaskResult = await TryUpdateTaskResultAsync( dequeuedTask, conn, tx );
						if ( dequeuedTaskResult != null )
						{
							await tx.CommitAsync();
							dequeuedTaskToken = new PostgreSqlQueuedTaskToken( dequeuedTask,
								dequeuedTaskResult,
								now );
						}
					}

					if ( dequeuedTaskToken == null )
						await tx.RollbackAsync();
				}
			}
			finally
			{
				//If no task has been found, attempt to 
				//  release all locks held by this connection 
				//  and also close it
				if ( dequeuedTaskToken == null && conn != null )
					await conn.UnlockAllAsync();

				if ( conn != null )
				{
					await conn.CloseAsync();
					conn.Dispose();
				}
			}

			return dequeuedTaskToken;
		}

		private async Task<QueuedTask> TryDequeueTaskAsync ( string[] selectTaskTypes,
			AbstractTimestamp now,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			QueuedTask dequeuedTask;
			Guid[] excludeLockedTaskIds =
				new Guid[ 0 ];

			using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand( mTaskDequeueSql, conn, tx ) )
			{
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
				}
			}

			return dequeuedTask;
		}

		private async Task<bool> TryAcquireTaskAsync ( QueuedTask dequeuedTask,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			using ( NpgsqlCommand removeCmd = new NpgsqlCommand( mTaskAcquireSql, conn, tx ) )
			{
				removeCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					dequeuedTask.Id );

				await removeCmd.PrepareAsync();
				return await removeCmd.ExecuteNonQueryAsync() == 1;
			}
		}

		private async Task<QueuedTaskResult> TryUpdateTaskResultAsync ( QueuedTask dequeuedTask,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			QueuedTaskResult dequeuedTaskResult = null;
			using ( NpgsqlCommand addOrUpdateResultCmd = new NpgsqlCommand( mTaskResultUpdateSql, conn, tx ) )
			{
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_id",
					NpgsqlDbType.Uuid,
					dequeuedTask.Id );
				addOrUpdateResultCmd.Parameters.AddWithValue( "t_status",
					NpgsqlDbType.Integer,
					( int )QueuedTaskStatus.Processing );

				await addOrUpdateResultCmd.PrepareAsync();
				using ( NpgsqlDataReader resultRdr = await addOrUpdateResultCmd.ExecuteReaderAsync() )
				{
					if ( await resultRdr.ReadAsync() )
						dequeuedTaskResult = await resultRdr.ReadQueuedTaskResultAsync();

					if ( dequeuedTaskResult != null )

						mLogger.Debug( "Successfully dequeued, acquired and initialized/updated task result." );
					else
						mLogger.Debug( "Failed to initialize or update task result. Will release lock..." );
				}
			}

			return dequeuedTaskResult;
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
