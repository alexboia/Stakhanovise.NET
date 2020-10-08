using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueProducerTests : BaseTestWithConfiguration
	{
		private TaskQueueOptions mProducerOptions;

		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueProducerTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mProducerOptions = TestOptions
				.GetDefaultTaskQueueProducerOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mProducerOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[SetUp]
		public async Task TestSetUp ()
		{
			await mDataSource.SeedData();
			await Task.Delay( 100 );
		}

		[TearDown]
		public async Task TestTearDown ()
		{
			await mDataSource.ClearData();
			await Task.Delay( 100 );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue_NewTask ()
		{
			Faker faker =
				new Faker();

			ManualResetEvent notificationWaitHandle =
				new ManualResetEvent( false );

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			AbstractTimestamp postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => postedAt );

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
				notificationWaitHandle.Set();

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer = CreateTaskQueueConsumer( () => postedAt ) )
			using ( TaskQueueMetricsDiff diff = new TaskQueueMetricsDiff( async () => await taskQueueInfo.ComputeMetricsAsync() ) )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				//Capture previous metrics
				await diff.CaptureInitialMetricsAsync();

				IQueuedTask queuedTask = await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: nameof( Test_CanEnqueue_NewTask ),
					priority: faker.Random.Int( 1, 100 ) );

				Assert.NotNull( queuedTask );
				await Assert_ResultAddedOrUpdatedCorrectly( queuedTask );

				notificationWaitHandle.WaitOne();

				await taskQueueConsumer.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueueConsumer.IsReceivingNewTaskUpdates );

				taskQueueConsumer.ClearForDequeue -=
					handleClearForDequeue;

				//Check that new metrics differ from the previous ones as expected
				await diff.CaptureNewMetricsAndAssertCorrectDiff( delta: new TaskQueueMetrics(
					totalUnprocessed: 1,
					totalProcessing: 0,
					totalErrored: 0,
					totalFaulted: 0,
					totalFataled: 0,
					totalProcessed: 0 ) );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue_RepostExistingTask ()
		{
			Faker faker =
				new Faker();

			AbstractTimestamp postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => postedAt );

			foreach ( IQueuedTaskToken token in mDataSource.CanBeRepostedSeededTaskTokens )
			{
				using ( TaskQueueMetricsDiff diff = new TaskQueueMetricsDiff( async () => await taskQueueInfo.ComputeMetricsAsync() ) )
				{
					QueuedTaskStatus prevStatus = token
						.LastQueuedTaskResult
						.Status;

					await diff.CaptureInitialMetricsAsync();

					QueuedTaskInfo repostTaskInfo = new QueuedTaskInfo()
					{
						Id = token.DequeuedTask.Id,
						Priority = faker.Random.Int( 1, 100 ),
						Payload = token.DequeuedTask.Payload,
						Source = nameof( Test_CanEnqueue_RepostExistingTask ),
						Type = token.DequeuedTask.Type,
						LockedUntil = postedAt.Ticks + faker.Random.Long( 10, 1000 )
					};

					await mDataSource.RemoveQueuedTaskByIdAsync( token.DequeuedTask.Id );

					IQueuedTask requeuedTask = await taskQueueProducer
						.EnqueueAsync( repostTaskInfo );

					Assert.NotNull( requeuedTask );
					await Assert_ResultAddedOrUpdatedCorrectly( requeuedTask );

					await diff.CaptureNewMetricsAndAssertCorrectDiff( delta: new TaskQueueMetrics(
						totalUnprocessed: prevStatus != QueuedTaskStatus.Unprocessed ? 1 : 0,
						totalProcessing: prevStatus == QueuedTaskStatus.Processing ? -1 : 0,
						totalErrored: prevStatus == QueuedTaskStatus.Error ? -1 : 0,
						totalFaulted: prevStatus == QueuedTaskStatus.Faulted ? -1 : 0,
						totalFataled: prevStatus == QueuedTaskStatus.Fatal ? -1 : 0,
						totalProcessed: prevStatus == QueuedTaskStatus.Processed ? -1 : 0 ) );
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_EnqueueDoesNotChangePeekResult_SingleConsumer ()
		{
			int futureTicks = 1;
			IQueuedTask peekTask = null,
				rePeekTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			PostgreSqlTaskQueueProducer taskQueueProducer = CreateTaskQueueProducer( () => mDataSource
				.LastPostedAt
				.AddTicks( 1 ) );

			PostgreSqlTaskQueueInfo taskQueueInfo = CreateTaskQueueInfo( () => mDataSource
				.LastPostedAt
				.AddTicks( futureTicks++ ) );

			peekTask = await taskQueueInfo.PeekAsync();
			Assert.NotNull( peekTask );

			await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
				source: nameof( Test_EnqueueDoesNotChangePeekResult_SingleConsumer ),
				priority: 0 );

			rePeekTask = await taskQueueInfo.PeekAsync();
			Assert.NotNull( rePeekTask );

			//Placing a new element in a queue occurs at its end, 
			//  so peeking must not be affected 
			//  if no other operation occurs
			Assert.AreEqual( peekTask.Id,
				rePeekTask.Id );

		}

		private async Task Assert_ResultAddedOrUpdatedCorrectly ( IQueuedTask queuedTask )
		{
			IQueuedTaskResult queuedTaskResult = await mDataSource
				.GetQueuedTaskResultByIdAsync( queuedTask.Id );

			Assert.NotNull( queuedTaskResult );
			Assert.NotNull( queuedTaskResult.Payload );

			Assert.AreEqual( queuedTask.Id,
				queuedTaskResult.Id );
			Assert.AreEqual( queuedTask.Type,
				queuedTaskResult.Type );
			Assert.AreEqual( queuedTask.Source,
				queuedTaskResult.Source );
			Assert.AreEqual( queuedTask.Priority,
				queuedTaskResult.Priority );
			Assert.AreEqual( queuedTask.PostedAt,
				queuedTaskResult.PostedAt );
			Assert.LessOrEqual( Math.Abs( ( queuedTask.PostedAtTs - queuedTaskResult.PostedAtTs ).TotalMilliseconds ),
				10 );
			Assert.AreEqual( QueuedTaskStatus.Unprocessed,
				queuedTaskResult.Status );
		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
