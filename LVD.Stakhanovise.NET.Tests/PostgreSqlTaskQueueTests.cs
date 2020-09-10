using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using Npgsql;
using NUnit.Framework;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueTests : BaseTestWithConfiguration
	{
		private const int QUEUE_LOCK_POOL_SIZE = 10;

		private const int QUEUE_CONNECTION_KEEPALIVE = 5;

		private const int QUEUE_FAULT_ERROR_THRESHOLD_COUNT = 5;

		private QueuedTaskMapping mQueuedTaskMap = new QueuedTaskMapping();

		private int mNumUnProcessedTasks = 5;

		private int mNumErroredTasks = 3;

		private int mNumFaultedTasks = 5;

		private int mNumFatalTasks = 1;

		private int mNumAuthorizationFailedTasks = 7;

		private List<QueuedTask> mSeededTasks = new List<QueuedTask>();

		private QueuedTaskStatus[] mDequeueWithStatuses = new QueuedTaskStatus[] {
			QueuedTaskStatus.Unprocessed,
			QueuedTaskStatus.Error,
			QueuedTaskStatus.Faulted
		};

		[SetUp]
		public async Task TestSetUp ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			using ( NpgsqlTransaction tx = db.BeginTransaction() )
			{
				DateTimeOffset now = DateTimeOffset.Now;
				List<QueuedTask> faultedTasks = GenerateFaultedTasks( now );
				List<QueuedTask> fataledTasks = GenerateFataledTasks( now );
				List<QueuedTask> erroredTasks = GenerateErroredTasks( now );
				List<QueuedTask> unprocessedTasks = GenerateUnprocessedTasks( now );

				mSeededTasks.Clear();
				await InsertTaskDataAsync( db, unprocessedTasks, tx );
				await InsertTaskDataAsync( db, erroredTasks, tx );
				await InsertTaskDataAsync( db, fataledTasks, tx );
				await InsertTaskDataAsync( db, faultedTasks, tx );

				await tx.CommitAsync();
			}

			await Task.Delay( 100 );
		}

		[TearDown]
		public async Task TestTearDown ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				mSeededTasks.Clear();
				await db.QueryFactory()
					.Query( mQueuedTaskMap.TableName )
					.DeleteAsync();
			}

			await Task.Delay( 100 );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanStartStopReceivingNewTaskNotificationUpdates ()
		{
			bool notificationReceived = false;
			ManualResetEvent notificationWaitHandle = new ManualResetEvent( false );

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				taskQueue.ClearForDequeue += ( s, e ) =>
				{
					notificationReceived = true;
					notificationWaitHandle.Set();
				};

				await taskQueue.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await SendNewTaskNotificationAsync();
				notificationWaitHandle.WaitOne();
				Assert.IsTrue( notificationReceived );

				await taskQueue.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanPeek ()
		{
			QueuedTask actualTopOfQueue;
			QueuedTask expectedTopOfQueue = ExpectedTopOfQueueTask;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				actualTopOfQueue = await taskQueue.PeekAsync();
				Assert.NotNull( actualTopOfQueue );
				Assert.AreEqual( expectedTopOfQueue.Id, actualTopOfQueue.Id );
			}
		}

		[Test]
		public async Task Test_CanComputeQueueMetrics ()
		{
			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				TaskQueueMetrics metrics = await taskQueue
					.ComputeMetricsAsync();

				Assert.NotNull( metrics );
				Assert.AreEqual( mNumUnProcessedTasks, metrics.TotalUnprocessed );
				Assert.AreEqual( mNumErroredTasks, metrics.TotalErrored );
				Assert.AreEqual( mNumFaultedTasks, metrics.TotalFaulted );
				Assert.AreEqual( mNumFatalTasks, metrics.TotalFataled );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanDequeue ()
		{
			List<Guid> taskIds =
				new List<Guid>();

			List<long> lockHandleIds =
				new List<long>();

			QueuedTask newTask = null,
				previousTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				for ( int i = 0; i < QUEUE_LOCK_POOL_SIZE; i++ )
				{
					newTask = await taskQueue.DequeueAsync( taskType );

					Assert.NotNull( newTask );
					Assert.IsFalse( taskIds.Contains( newTask.Id ) );

					taskIds.Add( newTask.Id );
					lockHandleIds.Add( newTask.LockHandleId );

					if ( previousTask != null )
						Assert.GreaterOrEqual( newTask.PostedAt, previousTask.PostedAt );

					previousTask = newTask;
				}

				//Double check that the locks are being held
				using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
				{
					foreach ( long lockHandleId in lockHandleIds )
						Assert.IsTrue( await db.IsAdvisoryLockHeldAsync( lockHandleId ) );
				}

				//Check that, when reaching the maximum number 
				//  of tasks we can dequeue, 
				//  no more dequeue-ing may take place
				newTask = await taskQueue.DequeueAsync( taskType );
				Assert.Null( newTask );
			}
		}

		[Test]
		public async Task Test_CanNotifyTaskCompleted ()
		{
			QueuedTask newTask = null,
				asCompletedTask = null,
				asReadCompletedTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				newTask = await taskQueue.DequeueAsync( taskType );
				Assert.NotNull( newTask );

				asCompletedTask = await taskQueue.NotifyTaskCompletedAsync( newTask.Id,
					new TaskExecutionResult( newTask ) );

				AssertTaskCompleted( asCompletedTask );
			}

			//Do a database lookup to verify 
			//  that the data has been updated accordingly
			asReadCompletedTask = await GetQueuedTaskByIdAsync( asCompletedTask.Id );
			AssertTaskCompleted( asReadCompletedTask );
		}

		[Test]
		public async Task Test_CanNotifiyTaskErrored ()
		{
			QueuedTask newTask = null,
				asErroredTask = null,
				asReadErroredTask = null;

			QueuedTaskStatus previousStatus;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				newTask = await taskQueue.DequeueAsync( taskType );
				Assert.NotNull( newTask );

				previousStatus = newTask.Status;
				asErroredTask = await taskQueue.NotifyTaskErroredAsync( newTask.Id,
					result: new TaskExecutionResult( newTask,
					   error: new QueuedTaskError( new InvalidOperationException( "Sample error" ) ),
					   isRecoverable: false ) );

				AssertTaskErrored( asErroredTask, previousStatus );
			}

			asReadErroredTask = await GetQueuedTaskByIdAsync( asErroredTask.Id );
			AssertTaskErrored( asReadErroredTask, previousStatus );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue ()
		{
			bool notificationReceived = false;
			ManualResetEvent notificationWaitHandle = new ManualResetEvent( false );

			TaskQueueMetrics previousMetrics,
				newMetrics;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				taskQueue.ClearForDequeue += ( s, e ) =>
				{
					notificationReceived = true;
					notificationWaitHandle.Set();
				};

				await taskQueue.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				previousMetrics = await taskQueue.ComputeMetricsAsync();
				Assert.NotNull( previousMetrics );

				await taskQueue.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: GetType().FullName,
					priority: 0 );

				notificationWaitHandle.WaitOne();
				Assert.IsTrue( notificationReceived );

				newMetrics = await taskQueue.ComputeMetricsAsync();
				Assert.NotNull( newMetrics );

				//One way to mirror the change is 
				//  to compare the before & after metrics
				Assert.AreEqual( previousMetrics.TotalErrored,
					newMetrics.TotalErrored );
				Assert.AreEqual( previousMetrics.TotalFataled,
					newMetrics.TotalFataled );
				Assert.AreEqual( previousMetrics.TotalFaulted,
					newMetrics.TotalFaulted );
				Assert.AreEqual( previousMetrics.TotalUnprocessed + 1,
					newMetrics.TotalUnprocessed );

				await taskQueue.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_PeekMatchesDequeuedItem_SingleConsumer ()
		{
			QueuedTask peekTask = null,
				dequeuedTask = null;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				peekTask = await taskQueue.PeekAsync();
				Assert.NotNull( peekTask );

				dequeuedTask = await taskQueue.DequeueAsync();
				Assert.NotNull( dequeuedTask );

				Assert.AreEqual( peekTask.Id, dequeuedTask.Id );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_DequeueChangesPeekResult_SingleConsumer ()
		{
			QueuedTask peekTask = null,
				rePeekTask = null,
				dequeuedTask = null;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				peekTask = await taskQueue.PeekAsync();
				Assert.NotNull( peekTask );

				dequeuedTask = await taskQueue.DequeueAsync();
				Assert.NotNull( dequeuedTask );

				rePeekTask = await taskQueue.PeekAsync();
				Assert.NotNull( rePeekTask );

				//Removing a new element from the queue 
				//  occurs at the beginning of the queue,
				//  so peeking must yield a different result
				//  than before dequeue-ing
				Assert.AreNotEqual( rePeekTask.Id, peekTask.Id );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_EnqueueDoesNotChangePeekResult_SingleConsumer ()
		{
			QueuedTask peekTask = null,
				rePeekTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueue taskQueue = CreateTaskQueue() )
			{
				peekTask = await taskQueue.PeekAsync();
				Assert.NotNull( peekTask );

				await taskQueue.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: GetType().FullName,
					priority: 0 );

				rePeekTask = await taskQueue.PeekAsync();
				Assert.NotNull( rePeekTask );

				//Placing a new element in a queue occurs at its end, 
				//  so peeking must not be affected 
				//  if no other operation occurs
				Assert.AreEqual( peekTask.Id, rePeekTask.Id );
			}
		}

		private List<QueuedTask> GenerateUnprocessedTasks ( DateTimeOffset now )
		{
			List<QueuedTask> unprocessedTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumUnProcessedTasks; i++ )
			{
				unprocessedTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumUnProcessedTasks ),
					PostedAt = now,
					RepostedAt = now,
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Unprocessed,
					Priority = 0
				} );
			}

			return unprocessedTasks;
		}

		private List<QueuedTask> GenerateErroredTasks ( DateTimeOffset now )
		{
			List<QueuedTask> erroredTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumErroredTasks; i++ )
			{
				erroredTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumErroredTasks ),
					PostedAt = now.AddSeconds( 1 ),
					RepostedAt = now.AddSeconds( 1 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Error,
					FirstProcessingAttemptedAt = DateTimeOffset.Now,
					LastProcessingAttemptedAt = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: error" ) ),
					ErrorCount = Math.Abs( QUEUE_FAULT_ERROR_THRESHOLD_COUNT - i ),
					Priority = 0
				} );
			}

			return erroredTasks;
		}

		private List<QueuedTask> GenerateFataledTasks ( DateTimeOffset now )
		{
			List<QueuedTask> fataledTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumFatalTasks; i++ )
			{
				fataledTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFatalTasks ),
					PostedAt = now.AddSeconds( 2 ),
					RepostedAt = now.AddSeconds( 2 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Fatal,
					FirstProcessingAttemptedAt = DateTimeOffset.Now,
					LastProcessingAttemptedAt = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: fatal" ) ),
					ErrorCount = QUEUE_FAULT_ERROR_THRESHOLD_COUNT + i,
					Priority = 0
				} );
			}

			return fataledTasks;
		}

		private List<QueuedTask> GenerateFaultedTasks ( DateTimeOffset now )
		{
			List<QueuedTask> faultedTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumFaultedTasks; i++ )
			{
				faultedTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFaultedTasks ),
					PostedAt = now.AddSeconds( 3 ),
					RepostedAt = now.AddSeconds( 3 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Faulted,
					FirstProcessingAttemptedAt = DateTimeOffset.Now,
					LastProcessingAttemptedAt = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: faulted" ) ),
					ErrorCount = QUEUE_FAULT_ERROR_THRESHOLD_COUNT,
					Priority = 0
				} );
			}

			return faultedTasks;
		}

		private async Task InsertTaskDataAsync ( NpgsqlConnection db, IEnumerable<QueuedTask> queuedTasks, NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertData;

			foreach ( QueuedTask queuedTask in queuedTasks )
			{
				insertData = new Dictionary<string, object>()
				{
					{ mQueuedTaskMap.IdColumnName,
						queuedTask.Id },
					{ mQueuedTaskMap.PayloadColumnName,
						queuedTask.Payload.ToJson(includeTypeInformation: true) },
					{ mQueuedTaskMap.TypeColumnName,
						queuedTask.Type },

					{ mQueuedTaskMap.StatusColumnName,
						queuedTask.Status },
					{ mQueuedTaskMap.SourceColumnName,
						queuedTask.Source },
					{ mQueuedTaskMap.PriorityColumnName,
						queuedTask.Priority },

					{ mQueuedTaskMap.PostedAtColumnName,
						queuedTask.PostedAt },
					{ mQueuedTaskMap.RepostedAtColumnName,
						queuedTask.RepostedAt },

					{ mQueuedTaskMap.ErrorCountColumnName,
						queuedTask.ErrorCount },
					{ mQueuedTaskMap.LastErrorColumnName,
						queuedTask.LastError.ToJson() },
					{ mQueuedTaskMap.LastErrorIsRecoverableColumnName,
						queuedTask.LastErrorIsRecoverable },

					{ mQueuedTaskMap.FirstProcessingAttemptedAtColumnName,
						queuedTask.FirstProcessingAttemptedAt },
					{ mQueuedTaskMap.LastProcessingAttemptedAtColumnName,
						queuedTask.LastProcessingAttemptedAt },
					{ mQueuedTaskMap.ProcessingFinalizedAtColumnName,
						queuedTask.ProcessingFinalizedAt }
				};

				await db.QueryFactory()
					.Query( mQueuedTaskMap.TableName )
					.InsertAsync( insertData, tx );

				mSeededTasks.AddRange( queuedTasks );
			}
		}

		private void AssertTaskCompleted ( QueuedTask testTask )
		{
			Assert.NotNull( testTask );
			Assert.IsTrue( testTask.FirstProcessingAttemptedAt.HasValue );
			Assert.IsTrue( testTask.LastProcessingAttemptedAt.HasValue );
			Assert.IsTrue( testTask.ProcessingFinalizedAt.HasValue );
			Assert.AreEqual( QueuedTaskStatus.Processed,
				testTask.Status );
		}

		private void AssertTaskErrored ( QueuedTask testTask, QueuedTaskStatus previousStatus )
		{
			Assert.NotNull( testTask.LastError );
			Assert.Greater( testTask.ErrorCount, 0 );

			Assert.IsTrue( testTask.FirstProcessingAttemptedAt.HasValue );
			Assert.IsTrue( testTask.LastProcessingAttemptedAt.HasValue );
			Assert.IsTrue( testTask.FirstProcessingAttemptedAt.HasValue );

			if ( testTask.ErrorCount >= QUEUE_FAULT_ERROR_THRESHOLD_COUNT )
			{
				if ( previousStatus == QueuedTaskStatus.Error )
					Assert.AreEqual( testTask.Status, QueuedTaskStatus.Faulted );
				else if ( previousStatus == QueuedTaskStatus.Faulted )
					Assert.AreEqual( testTask.Status, QueuedTaskStatus.Fatal );
			}
			else
				Assert.AreEqual( testTask.Status, QueuedTaskStatus.Error );
		}

		private async Task<QueuedTask> GetQueuedTaskByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = db
					.QueryFactory()
					.Query( mQueuedTaskMap.TableName )
					.Select( "*" )
					.Where( mQueuedTaskMap.IdColumnName, "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskAsync( mQueuedTaskMap )
						: null;
			}
		}

		private PostgreSqlTaskQueue CreateTaskQueue ()
		{
			PostgreSqlTaskQueue taskQueue = new PostgreSqlTaskQueue( mQueuedTaskMap,
				connectionString: ConnectionString,
				lockPookSize: QUEUE_LOCK_POOL_SIZE,
				keepalive: QUEUE_CONNECTION_KEEPALIVE );

			taskQueue.FaultErrorThresholdCount = QUEUE_FAULT_ERROR_THRESHOLD_COUNT;
			return taskQueue;
		}

		private async Task SendNewTaskNotificationAsync ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
				await db.NotifyAsync( mQueuedTaskMap.NewTaskNotificaionChannelName, null );
			await Task.Delay( 100 );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( ConnectionString );
			await db.OpenAsync();
			return db;
		}

		private QueuedTask ExpectedTopOfQueueTask
			=> mSeededTasks.Where( t => mDequeueWithStatuses.Contains( t.Status ) )
				.OrderByDescending( t => t.Priority )
				.OrderBy( t => t.PostedAt )
				.OrderBy( t => t.LockHandleId )
				.FirstOrDefault();

		private string ConnectionString => GetConnectionString( "baseTestDbConnectionString" );
	}
}
