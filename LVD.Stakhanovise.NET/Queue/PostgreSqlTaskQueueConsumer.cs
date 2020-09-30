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

		private ConcurrentDictionary<Guid, IQueuedTaskToken> mDequeuedTokens =
			new ConcurrentDictionary<Guid, IQueuedTaskToken>();

		private int[] mDequeueWithStatuses;

		public PostgreSqlTaskQueueConsumer ( TaskQueueConsumerOptions options )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			mOptions = options;

			mDequeueWithStatuses = mOptions.ProcessWithStatuses
				.Select( s => ( int )s )
				.ToArray();

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

		public async Task<IQueuedTaskToken> DequeueAsync ( AbstractTimestamp now, params string[] selectTaskTypes )
		{
			NpgsqlConnection conn = null;
			QueuedTask dequedTask = null;
			IQueuedTaskToken dequeuedTaskToken = null;

			CheckNotDisposedOrThrow();

			Guid[] excludeLockedTaskIds = mDequeuedTokens.Keys
				.ToArray();

			try
			{
				mLogger.DebugFormat( "Begin dequeue task types: {0}; task statuses = {1}; excluded ids = {2}.",
					string.Join<string>( ",", selectTaskTypes ),
					string.Join<int>( ",", mDequeueWithStatuses ),
					string.Join<Guid>( ",", excludeLockedTaskIds ) );

				conn = await OpenQueueConnectionAsync();
				if ( conn == null )
					return null;

				using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand() )
				{
					dequeueCmd.Connection = conn;
					dequeueCmd.CommandText = $"SELECT tq.* FROM { mOptions.Mapping.DequeueFunctionName }(@statuses, @types, @excluded, @now) tq";

					dequeueCmd.Parameters.AddWithValue( "statuses",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Integer,
						value: mDequeueWithStatuses );

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

					using ( NpgsqlDataReader taskReader = await dequeueCmd.ExecuteReaderAsync() )
					{
						dequedTask = await taskReader.ReadAsync()
							? await taskReader.ReadQueuedTaskAsync( mOptions.Mapping )
							: null;

						//If a new task has been found, save it 
						//	along with the database connection 
						//	that holds the lock
						if ( dequedTask != null )
						{
							dequeuedTaskToken = new PostgreSqlQueuedTaskToken( dequedTask,
								sourceConnection: conn,
								dequeuedAt: now,
								options: mOptions );

							dequeuedTaskToken.TokenReleased +=
								HandleTaskTokenReleased;

							mDequeuedTokens.TryAdd( dequedTask.Id,
								dequeuedTaskToken );

							mLogger.DebugFormat( "Found with id = {0} in database queue.",
								dequedTask.Id );
						}
						else
							mLogger.Debug( "No task dequeued because no task found." );
					}
				}
			}
			finally
			{
				//If no task has been found, attempt to 
				//  release all locks held by this connection 
				//  and also close it
				if ( dequedTask == null && conn != null )
				{
					await conn.UnlockAllAsync();
					conn.Close();
					conn.Dispose();
				}
			}

			return dequeuedTaskToken;
		}

		private void HandleTaskTokenReleased ( object sender, TokenReleasedEventArgs e )
		{
			mDequeuedTokens.TryRemove( e.QueuedTaskId, out IQueuedTaskToken taskToken );
			taskToken.TokenReleased -= HandleTaskTokenReleased;
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

					mDequeuedTokens.Clear();
					mDequeuedTokens = null;
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
	}
}
