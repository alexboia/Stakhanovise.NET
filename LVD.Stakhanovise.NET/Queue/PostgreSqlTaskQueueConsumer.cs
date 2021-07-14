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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueConsumer : ITaskQueueConsumer, IAppMetricsProvider, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsDisposed;

		private TaskQueueConsumerOptions mOptions;

		private string mSignalingConnectionString;

		private PostgreSqlTaskQueueNotificationListener mNotificationListener;

		private string mTaskDequeueSql;

		private string mTaskAcquireSql;

		private string mTaskResultUpdateSql;

		private ITimestampProvider mTimestampProvider;

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.QueueConsumerDequeueCount, 0 ),
			new AppMetric( AppMetricId.QueueConsumerMaximumDequeueDuration, long.MinValue ),
			new AppMetric( AppMetricId.QueueConsumerMinimumDequeueDuration, long.MaxValue ),
			new AppMetric( AppMetricId.QueueConsumerTotalDequeueDuration, 0 )
		);

		public PostgreSqlTaskQueueConsumer ( TaskQueueConsumerOptions options, ITimestampProvider timestampProvider )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );
			if ( timestampProvider == null )
				throw new ArgumentNullException( nameof( timestampProvider ) );

			mOptions = options;
			mTimestampProvider = timestampProvider;

			mSignalingConnectionString = options.ConnectionOptions
				.ConnectionString
				.DeriveSignalingConnectionString( options );

			mNotificationListener = new PostgreSqlTaskQueueNotificationListener( mSignalingConnectionString,
				options.Mapping.NewTaskNotificationChannelName );

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
			return await mOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
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
			//See https://dba.stackexchange.com/questions/69471/postgres-update-limit-1/69497#69497
			return $@"SELECT tq.* FROM { mapping.DequeueFunctionName }(@types, @excluded, @ref_now) tq";
		}

		private string GetTaskAcquireSql ( QueuedTaskMapping mapping )
		{
			return $@"DELETE FROM {mapping.QueueTableName} 
				WHERE task_id = @t_id";
		}

		private string GetTaskResultUpdateSql ( QueuedTaskMapping mapping )
		{
			return $@"UPDATE {mapping.ResultsQueueTableName} SET
					task_status = @t_status,
					task_first_processing_attempted_at_ts = COALESCE(task_first_processing_attempted_at_ts, NOW()),
					task_last_processing_attempted_at_ts = NOW() 
				WHERE task_id = @t_id
				RETURNING *";
		}

		private void IncrementDequeueCount ( TimeSpan duration )
		{
			long durationMilliseconds = ( long )Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerDequeueCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerTotalDequeueDuration,
				m => m.Add( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerMinimumDequeueDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerMaximumDequeueDuration,
				m => m.Max( durationMilliseconds ) );
		}

		public async Task<IQueuedTaskToken> DequeueAsync ( params string[] selectTaskTypes )
		{
			NpgsqlConnection conn = null;
			QueuedTask dequeuedTask = null;
			QueuedTaskResult dequeuedTaskResult = null;
			PostgreSqlQueuedTaskToken dequeuedTaskToken = null;

			MonotonicTimestamp startDequeue;
			DateTimeOffset refNow = mTimestampProvider.GetNow();

			CheckNotDisposedOrThrow();

			try
			{
				mLogger.DebugFormat( "Begin dequeue task. Looking for types: {0}.",
					string.Join<string>( ",", selectTaskTypes ) );

				startDequeue = MonotonicTimestamp
					.Now();

				conn = await OpenQueueConnectionAsync();
				if ( conn == null )
					return null;

				using ( NpgsqlTransaction tx = conn.BeginTransaction( IsolationLevel.ReadCommitted ) )
				{
					//1. Dequeue means that we acquire lock on a task in the queue
					//	with the guarantee that nobody else did, and respecting 
					//	the priority and static locks (basically the task_locked_until which says 
					//	that it should not be pulled out of the queue until the 
					//	current abstract time reaches that tick value)
					dequeuedTask = await TryDequeueTaskAsync( selectTaskTypes, refNow, conn, tx );
					if ( dequeuedTask != null )
					{
						//2. Mark the task as being "Processing" and pull result info
						//	The result is stored separately and it's what allows us to remove 
						//	the task from the queue at step #2, 
						//	whils also tracking it's processing status and previous results
						dequeuedTaskResult = await TryUpdateTaskResultAsync( dequeuedTask, conn, tx );
						if ( dequeuedTaskResult != null )
						{
							await tx.CommitAsync();
							dequeuedTaskToken = new PostgreSqlQueuedTaskToken( dequeuedTask,
								dequeuedTaskResult,
								refNow );
						}
					}

					if ( dequeuedTaskToken != null )
						IncrementDequeueCount( MonotonicTimestamp.Since( startDequeue ) );
					else
						await tx.RollbackAsync();

				}
			}
			finally
			{
				if ( conn != null )
				{
					await conn.CloseAsync();
					conn.Dispose();
				}
			}

			return dequeuedTaskToken;
		}

		private async Task<QueuedTask> TryDequeueTaskAsync ( string[] selectTaskTypes,
			DateTimeOffset refNow,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			QueuedTask dequeuedTask;
			using ( NpgsqlCommand dequeueCmd = new NpgsqlCommand( mTaskDequeueSql, conn, tx ) )
			{
				dequeueCmd.Parameters.AddWithValue( "types",
					parameterType: NpgsqlDbType.Array | NpgsqlDbType.Varchar,
					value: selectTaskTypes );

				dequeueCmd.Parameters.AddWithValue( "excluded",
					parameterType: NpgsqlDbType.Array | NpgsqlDbType.Uuid,
					value: NoExcludedTaskIds );

				dequeueCmd.Parameters.AddWithValue( "ref_now",
					parameterType: NpgsqlDbType.TimestampTz,
					value: refNow );

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

		public AppMetric QueryMetric ( AppMetricId metricId )
		{
			return AppMetricsCollection.JoinQueryMetric( metricId, 
				mMetrics,
				mNotificationListener );
		}

		public IEnumerable<AppMetric> CollectMetrics ()
		{
			return AppMetricsCollection.JoinCollectMetrics( mMetrics,
				mNotificationListener );
		}

		public bool IsReceivingNewTaskUpdates
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mNotificationListener.IsStarted;
			}
		}

		public ITimestampProvider TimestampProvider
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mTimestampProvider;
			}
		}

		public IEnumerable<AppMetricId> ExportedMetrics
		{
			get
			{
				return AppMetricsCollection.JoinExportedMetrics( mMetrics,
					mNotificationListener );
			}
		}

		private Guid[] NoExcludedTaskIds
			=> new Guid[ 0 ];
	}
}
