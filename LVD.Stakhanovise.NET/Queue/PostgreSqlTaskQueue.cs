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
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using LVD.Stakhanovise.NET.Model;
using Npgsql;
using LVD.Stakhanovise.NET.Helpers;
using System.Threading.Tasks;
using SqlKata;
using NpgsqlTypes;
using System.Linq;
using SqlKata.Execution;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueue : ITaskQueueConsumer,
		ITaskQueueProducer,
		ITaskQueueStats,
		IDisposable
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private int mLockPoolSize;

		private QueuedTaskMapping mQueuedTaskMapping;

		private string mSignalingConnectionString;

		private string mManagementConnectionString;

		private string mReadOnlyConnectionString;

		private ConcurrentDictionary<Guid, QueuedTaskToken> mTaskTokens
			= new ConcurrentDictionary<Guid, QueuedTaskToken>();

		private PostgreSqlTaskQueueNotificationListener mNotificationListener;

		private int[] mDequeueWithStatuses = new int[] {
			(int)QueuedTaskStatus.Unprocessed,
			(int)QueuedTaskStatus.Error,
			(int)QueuedTaskStatus.Faulted
		};

		private int mFaultErrorThresholdCount = 5;

		private bool mIsDisposed = false;

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueue ), "Cannot reuse a disposed task queue" );
		}

		private async Task<NpgsqlConnection> OpenReadOnlyConnectionAsync ()
		{
			NpgsqlConnection connection = new NpgsqlConnection( mReadOnlyConnectionString );
			await connection.OpenAsync();
			return connection;
		}

		private async Task<NpgsqlConnection> OpenManagementConnectionAsync ()
		{
			NpgsqlConnection connection = new NpgsqlConnection( mManagementConnectionString );
			await connection.OpenAsync();
			return connection;
		}

		private async Task<NpgsqlConnection> OpenSignalingConnectionAsync ()
		{
			NpgsqlConnection connection = new NpgsqlConnection( mSignalingConnectionString );
			await connection.OpenAsync();
			return connection;
		}

		private void NotifyClearForDequeue ( ClearForDequeReason reason )
		{
			EventHandler<ClearForDequeueEventArgs> eventHandler = ClearForDequeue;
			if ( eventHandler != null )
				eventHandler( this, new ClearForDequeueEventArgs( reason ) );
		}

		public PostgreSqlTaskQueue ( QueuedTaskMapping queuedTaskMap,
			string connectionString,
			int lockPookSize,
			int keepalive )
		{
			NpgsqlConnectionStringBuilder connectionStringInfo;

			if ( string.IsNullOrWhiteSpace( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			if ( queuedTaskMap == null )
				throw new ArgumentNullException( nameof( queuedTaskMap ) );

			if ( keepalive < 0 )
				throw new ArgumentOutOfRangeException( nameof( keepalive ), "The keepalive interval must be >= 0" );

			//Parse the initial connection string as we need to:
			//  a) derive the management connection string from it
			//  b) derive the signaling connection string from it
			connectionStringInfo = new NpgsqlConnectionStringBuilder( connectionString );

			mSignalingConnectionString = DeriveSignalingConnectionString( connectionStringInfo,
				keepalive: keepalive );

			mManagementConnectionString = DeriveManagementConnectionString( connectionStringInfo,
				poolSize: lockPookSize * 2,
				keepalive: keepalive );

			mNotificationListener = new PostgreSqlTaskQueueNotificationListener( mSignalingConnectionString,
				queuedTaskMap.NewTaskNotificaionChannelName );

			mNotificationListener.ListenerConnectionRestored +=
				HandleListenerConnectionRestored;
			mNotificationListener.NewTaskPosted +=
				HandleNewTaskUpdateReceived;
			mNotificationListener.ListenerTimedOutWhileWaiting +=
				HandleListenerTimedOut;

			mReadOnlyConnectionString = connectionString;
			mQueuedTaskMapping = queuedTaskMap;
			mLockPoolSize = lockPookSize;
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

		private string DeriveManagementConnectionString ( NpgsqlConnectionStringBuilder info, int poolSize, int keepalive )
		{
			//The connection used for management will be 
			//  the same as the one used for read-only queue operation 
			//  with the notable exceptions that: 
			//  a) we need  to activate the Npgsql keepalive mechanism (see: http://www.npgsql.org/doc/keepalive.html)
			//  b) we need to configure the connection pool to match the required lock pool size

			NpgsqlConnectionStringBuilder managementConnectionStringInfo = info.Copy();

			managementConnectionStringInfo.Pooling = true;
			managementConnectionStringInfo.MinPoolSize = poolSize;
			managementConnectionStringInfo.MaxPoolSize = poolSize;
			managementConnectionStringInfo.KeepAlive = keepalive;

			return managementConnectionStringInfo.ToString();
		}

		private string DeriveSignalingConnectionString ( NpgsqlConnectionStringBuilder info, int keepalive )
		{
			//The connection used for signaling will be 
			//  the same as the one used for read-only queue operation 
			//  with the notable exceptions that: 
			//  a) we need  to activate the Npgsql keepalive mechanism (see: http://www.npgsql.org/doc/keepalive.html)
			//  b) we do not need a large pool - one connection will do

			NpgsqlConnectionStringBuilder signalingConnectionStringInfo = info.Copy();

			signalingConnectionStringInfo.Pooling = true;
			signalingConnectionStringInfo.MinPoolSize = 1;
			signalingConnectionStringInfo.MaxPoolSize = 1;
			signalingConnectionStringInfo.KeepAlive = keepalive;

			return signalingConnectionStringInfo.ToString();
		}

		public async Task StartReceivingNewTaskUpdatesAsync ()
		{
			CheckDisposedOrThrow();
			await mNotificationListener.StartAsync();
		}

		public async Task StopReceivingNewTaskUpdatesAsync ()
		{
			CheckDisposedOrThrow();
			await mNotificationListener.StopAsync();
		}

		public async Task<TaskQueueMetrics> ComputeMetricsAsync ()
		{
			long totalUnprocessed = 0,
				totalErrored = 0,
				totalFaulted = 0,
				totalFataled = 0,
				totalProcessed = 0;

			CheckDisposedOrThrow();

			Query query = new Query( $"{mQueuedTaskMapping.TableName} AS q" )
				.Select( $"q.{mQueuedTaskMapping.StatusColumnName}" )
				.SelectRaw( $"COUNT(q.{mQueuedTaskMapping.StatusColumnName}) AS task_status_count" )
				.GroupBy( $"q.{mQueuedTaskMapping.StatusColumnName}" );

			using ( NpgsqlConnection db = await OpenReadOnlyConnectionAsync() )
			using ( NpgsqlDataReader metricsReader = await db.ExecuteReaderAsync( query ) )
			{
				while ( metricsReader.Read() )
				{
					long count = await metricsReader.GetFieldValueAsync( "task_status_count",
						defaultValue: 0 );

					QueuedTaskStatus status = ( QueuedTaskStatus )( await metricsReader.GetFieldValueAsync( mQueuedTaskMapping.StatusColumnName,
						defaultValue: 0 ) );

					switch ( status )
					{
						case QueuedTaskStatus.Unprocessed:
							totalUnprocessed = count;
							break;
						case QueuedTaskStatus.Error:
							totalErrored = count;
							break;
						case QueuedTaskStatus.Faulted:
							totalFaulted = count;
							break;
						case QueuedTaskStatus.Fatal:
							totalFataled = count;
							break;
						case QueuedTaskStatus.Processed:
							totalProcessed = count;
							break;
					}
				}
			}

			return new TaskQueueMetrics( totalUnprocessed,
				totalErrored,
				totalFaulted,
				totalFataled,
				totalProcessed );
		}

		public async Task ReleaseLockAsync ( Guid queuedTaskId )
		{
			QueuedTaskToken acquiredToken = null;

			CheckDisposedOrThrow();
			if ( queuedTaskId.Equals( Guid.Empty ) )
				throw new ArgumentNullException( nameof( queuedTaskId ) );

			try
			{
				if ( !mTaskTokens.TryRemove( queuedTaskId, out acquiredToken ) )
					return;

				await acquiredToken.Connection.UnlockAsync( acquiredToken
					.QueuedTask
					.LockHandleId );
			}
			finally
			{
				acquiredToken?.Dispose();
			}
		}

		public async Task<QueuedTask> DequeueAsync ( params string[] selectTaskTypes )
		{
			NpgsqlConnection db = null;
			QueuedTask dequedTask = null;

			CheckDisposedOrThrow();

			Guid[] excludeLockedTaskIds = mTaskTokens.Keys
				.ToArray();

			//We have reached the maximum allowed lock pool size,
			//  exit without even trying to acquire a new task
			if ( excludeLockedTaskIds.Length >= mLockPoolSize )
				return null;

			try
			{
				mLogger.DebugFormat( "Begin dequeue task types: {0}; task statuses = {1}; excluded ids = {2}.",
					string.Join<string>( ",", selectTaskTypes ),
					string.Join<int>( ",", mDequeueWithStatuses ),
					string.Join<Guid>( ",", excludeLockedTaskIds ) );

				db = await OpenManagementConnectionAsync();

				using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand() )
				{
					dequeueCmd.Connection = db;
					dequeueCmd.CommandText = $"SELECT tq.* FROM { mQueuedTaskMapping.DequeueFunctionName }(@statuses, @types, @excluded) tq";

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
							? await taskReader.ReadQueuedTaskAsync( mQueuedTaskMapping )
							: null;

						//If a new task has been found, save it 
						//	along with the database connection 
						//	that holds the lock
						if ( dequedTask != null )
						{
							mTaskTokens.TryAdd( dequedTask.Id, new QueuedTaskToken( dequedTask, db ) );
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
				if ( dequedTask == null && db != null )
				{
					await db.UnlockAllAsync();
					db.Close();
					db.Dispose();
				}
			}

			return dequedTask;
		}

		public async Task<QueuedTask> EnqueueAsync<TPayload> ( TPayload payload,
			string source,
			int priority )
		{
			QueuedTask queuedTask = null;
			Dictionary<string, object> insertData;

			CheckDisposedOrThrow();

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
			queuedTask.PostedAt = DateTimeOffset.Now;
			queuedTask.RepostedAt = DateTimeOffset.Now;
			queuedTask.ErrorCount = 0;

			using ( NpgsqlConnection db = await OpenManagementConnectionAsync() )
			using ( NpgsqlTransaction tx = db.BeginTransaction() )
			{
				insertData = new Dictionary<string, object>()
				{
					{ mQueuedTaskMapping.IdColumnName,
						queuedTask.Id },
					{ mQueuedTaskMapping.PayloadColumnName,
						queuedTask.Payload.ToJson(includeTypeInformation: true) },
					{ mQueuedTaskMapping.TypeColumnName,
						queuedTask.Type },
					{ mQueuedTaskMapping.SourceColumnName,
						queuedTask.Source },
					{ mQueuedTaskMapping.PriorityColumnName,
						queuedTask.Priority },
					{ mQueuedTaskMapping.PostedAtColumnName,
						queuedTask.PostedAt },
					{ mQueuedTaskMapping.RepostedAtColumnName,
						queuedTask.RepostedAt },
					{ mQueuedTaskMapping.ErrorCountColumnName,
						queuedTask.ErrorCount },
					{ mQueuedTaskMapping.LastErrorIsRecoverableColumnName,
						queuedTask.LastErrorIsRecoverable },
					{ mQueuedTaskMapping.StatusColumnName,
						queuedTask.Status }
				};

				await db.QueryFactory()
					.Query( mQueuedTaskMapping.TableName )
					.InsertAsync( insertData, tx );

				await db.NotifyAsync( mQueuedTaskMapping
					.NewTaskNotificaionChannelName, tx );

				tx.Commit();
			}

			return queuedTask;
		}

		public async Task<QueuedTask> NotifyTaskCompletedAsync ( Guid queuedTaskId, TaskExecutionResult result )
		{
			QueuedTaskToken acquiredToken = null;
			Dictionary<string, object> updateData;

			CheckDisposedOrThrow();
			if ( queuedTaskId.Equals( Guid.Empty ) )
				throw new ArgumentNullException( nameof( queuedTaskId ) );

			try
			{
				//No lock found for the given task, exit
				//Removing the item (TryRemove) instead of just fetching it (TryGet) from the collection
				//  ensures that only one thread at a time 
				//  may request processing of a given task
				if ( !mTaskTokens.TryRemove( queuedTaskId, out acquiredToken ) )
					return null;

				//Mark the task as processed
				acquiredToken.QueuedTask
					.Processed();

				//Update database
				updateData = new Dictionary<string, object>()
				{
					{ mQueuedTaskMapping.StatusColumnName,
						acquiredToken.QueuedTask.Status },
					{ mQueuedTaskMapping.ProcessingFinalizedAtColumnName,
						acquiredToken.QueuedTask.ProcessingFinalizedAt },
					{ mQueuedTaskMapping.LastProcessingAttemptedAtColumnName,
						acquiredToken.QueuedTask.LastProcessingAttemptedAt },
					{ mQueuedTaskMapping.FirstProcessingAttemptedAtColumnName,
						acquiredToken.QueuedTask.FirstProcessingAttemptedAt }
				};

				await acquiredToken.Connection
					.QueryFactory()
						.Query( mQueuedTaskMapping.TableName )
						.Where( mQueuedTaskMapping.IdColumnName, "=", queuedTaskId )
						.UpdateAsync( updateData );

				//Release lock
				await acquiredToken.Connection.UnlockAsync( acquiredToken
					.QueuedTask
					.LockHandleId );

				return acquiredToken.QueuedTask;
			}
			catch ( Exception )
			{
				//If something failed, attempt to add it back 
				//  to the array of held locks
				if ( acquiredToken != null )
					mTaskTokens.TryAdd( queuedTaskId, acquiredToken );

				acquiredToken = null;
				throw;
			}
			finally
			{
				//Make sure the connection is cleaned-up, 
				//  if the task lock was found and no error occured 
				//  while processing the completion of the task
				if ( acquiredToken != null )
					acquiredToken.Dispose();
			}
		}

		public async Task<QueuedTask> NotifyTaskErroredAsync ( Guid queuedTaskId, TaskExecutionResult result )
		{
			QueuedTaskToken acquiredToken = null;
			Dictionary<string, object> updateData;

			CheckDisposedOrThrow();

			if ( queuedTaskId.Equals( Guid.Empty ) )
				throw new ArgumentNullException( nameof( queuedTaskId ) );

			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			try
			{
				//No lock found for the given task, exit
				//Removing the item (TryRemove) instead of just fetching it (TryGet) from the collection
				//  ensures that only one thread at a time 
				//  may request processing of a given task
				if ( !mTaskTokens.TryRemove( queuedTaskId, out acquiredToken ) )
					return null;

				//Mark the task as processed
				//acquiredToken.QueuedTask
				//	.HadError( result.Error, result.IsRecoverable );

				if ( acquiredToken.QueuedTask.ErrorCount >= mFaultErrorThresholdCount )
				{
					if ( acquiredToken.QueuedTask.Status == QueuedTaskStatus.Error )
						acquiredToken.QueuedTask.Faulted();
					else if ( acquiredToken.QueuedTask.Status == QueuedTaskStatus.Faulted )
						acquiredToken.QueuedTask.ProcessingFailedPermanently();
				}

				//Update database
				updateData = new Dictionary<string, object>()
				{
					{ mQueuedTaskMapping.StatusColumnName,
						acquiredToken.QueuedTask.Status },

					{ mQueuedTaskMapping.LastErrorColumnName,
						acquiredToken.QueuedTask.LastError.ToJson() },
					{ mQueuedTaskMapping.LastErrorIsRecoverableColumnName,
						acquiredToken.QueuedTask.LastErrorIsRecoverable },
					{ mQueuedTaskMapping.ErrorCountColumnName,
						acquiredToken.QueuedTask.ErrorCount },

					{ mQueuedTaskMapping.FirstProcessingAttemptedAtColumnName,
						acquiredToken.QueuedTask.FirstProcessingAttemptedAt },
					{ mQueuedTaskMapping.LastProcessingAttemptedAtColumnName,
						acquiredToken.QueuedTask.LastProcessingAttemptedAt },
					{ mQueuedTaskMapping.RepostedAtColumnName,
						acquiredToken.QueuedTask.RepostedAt }
				};

				await acquiredToken.Connection
					.QueryFactory()
						.Query( mQueuedTaskMapping.TableName )
						.Where( mQueuedTaskMapping.IdColumnName, "=", queuedTaskId )
						.UpdateAsync( updateData );

				//Release lock
				await acquiredToken.Connection.UnlockAsync( acquiredToken
					.QueuedTask
					.LockHandleId );

				return acquiredToken.QueuedTask;
			}
			catch ( Exception )
			{
				//If something failed, attempt to add it back 
				//  to the array of held locks
				if ( acquiredToken != null )
					mTaskTokens.TryAdd( queuedTaskId, acquiredToken );

				acquiredToken = null;
				throw;
			}
			finally
			{
				//Make sure the connection is cleaned-up, 
				//  if the task lock was found and no error occured 
				//  while processing the completion of the task
				if ( acquiredToken != null )
					acquiredToken.Dispose();
			}
		}

		public async Task<QueuedTask> PeekAsync ()
		{
			CheckDisposedOrThrow();

			//Tasks that have been dequeued must not appear 
			//  in front of the queue when peeking
			Guid[] excludeLockedTaskIds = mTaskTokens.Keys
				.ToArray();

			//This simply returns the latest item on top of the queue,
			//  without acquiring any lock

			Query peekQuery = new Query( $"{mQueuedTaskMapping.TableName} as q" )
				.Select( "q.*" )
				.WhereIn( $"q.{mQueuedTaskMapping.StatusColumnName}", mDequeueWithStatuses );

			if ( excludeLockedTaskIds.Length > 0 )
				peekQuery.WhereNotIn( $"{mQueuedTaskMapping.IdColumnName}", excludeLockedTaskIds );

			peekQuery.OrderByDesc( $"q.{mQueuedTaskMapping.PriorityColumnName}" )
				.OrderBy( $"q.{mQueuedTaskMapping.PostedAtColumnName}" )
				.OrderBy( $"q.{mQueuedTaskMapping.LockHandleIdColumnName}" )
				.Limit( 1 );

			using ( NpgsqlConnection db = await OpenReadOnlyConnectionAsync() )
			using ( NpgsqlDataReader taskReader = await db.ExecuteReaderAsync( peekQuery ) )
				return await taskReader.ReadAsync()
					? await taskReader.ReadQueuedTaskAsync( mQueuedTaskMapping )
					: null;
		}

		private void DisposeAcquiredLocks ()
		{
			ICollection<QueuedTaskToken> acquiredLocks = mTaskTokens.Values;

			foreach ( QueuedTaskToken acquiredLock in acquiredLocks )
				acquiredLock.Dispose();

			mTaskTokens.Clear();
		}

		private void DisposeWaitHandles ()
		{
			return;
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

					DisposeAcquiredLocks();
					DisposeWaitHandles();
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public int FaultErrorThresholdCount
		{
			get => mFaultErrorThresholdCount;
			set => mFaultErrorThresholdCount = value;
		}

		public bool IsReceivingNewTaskUpdates
			=> mNotificationListener.IsStarted;

		public int DequeuePoolSize
			=> mLockPoolSize;

		public QueuedTaskMapping QueuedTaskMapping
			=> mQueuedTaskMapping;
	}
}
