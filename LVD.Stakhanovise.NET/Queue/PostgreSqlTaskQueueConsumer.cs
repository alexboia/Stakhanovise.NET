using log4net;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueConsumer : ITaskQueueConsumer, IDisposable
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsDisposed;

		private TaskQueueOptions mOptions;

		private string mSignalingConnectionString;

		private string mQueueConnectionString;

		private PostgreSqlTaskQueueNotificationListener mNotificationListener;

		private ConcurrentDictionary<Guid, IQueuedTaskToken> mDequeuedTokens =
			new ConcurrentDictionary<Guid, IQueuedTaskToken>();

		private int[] mDequeueWithStatuses = new int[] {
			(int)QueuedTaskStatus.Unprocessed,
			(int)QueuedTaskStatus.Error,
			(int)QueuedTaskStatus.Faulted,
			(int)QueuedTaskStatus.Processing
		};

		public PostgreSqlTaskQueueConsumer ( TaskQueueOptions options )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			mOptions = options;
			mSignalingConnectionString = options.ConnectionString
				.DeriveSignalingConnectionString( options );
			mQueueConnectionString = options.ConnectionString
				.DeriveQueueConnectionString( options );

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
			return await mSignalingConnectionString.TryOpenConnectionAsync( mOptions.ConnectionRetryCount,
				mOptions.ConnectionRetryDelay );
		}

		private async Task<NpgsqlConnection> OpenQueueConnectionAsync ()
		{
			return await mQueueConnectionString.TryOpenConnectionAsync( mOptions.ConnectionRetryCount,
				mOptions.ConnectionRetryDelay );
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

			//We have reached the maximum allowed lock pool size,
			//  exit without even trying to acquire a new task
			if ( excludeLockedTaskIds.Length >= mOptions.DequeuePoolSize )
				return null;

			try
			{
				mLogger.DebugFormat( "Begin dequeue task types: {0}; task statuses = {1}; excluded ids = {2}.",
					string.Join<string>( ",", selectTaskTypes ),
					string.Join<int>( ",", mDequeueWithStatuses ),
					string.Join<Guid>( ",", excludeLockedTaskIds ) );

				conn = await OpenQueueConnectionAsync();

				using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand() )
				{
					dequeueCmd.Connection = conn;
					dequeueCmd.CommandText = $"SELECT tq.* FROM { mOptions.Mapping.DequeueFunctionName }(@statuses, @types, @excluded) tq";

					dequeueCmd.Parameters.AddWithValue( "statuses",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Integer,
						value: mDequeueWithStatuses );

					dequeueCmd.Parameters.AddWithValue( "types",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Varchar,
						value: selectTaskTypes );

					dequeueCmd.Parameters.AddWithValue( "excluded",
						parameterType: NpgsqlDbType.Array | NpgsqlDbType.Uuid,
						value: excludeLockedTaskIds );

					dequeueCmd.Prepare();

					using ( NpgsqlDataReader taskReader = ( NpgsqlDataReader )( await dequeueCmd.ExecuteReaderAsync() ) )
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
