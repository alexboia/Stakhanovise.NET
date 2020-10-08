using System;
using System.Collections.Generic;
using System.Text;
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
	public class PostgreSqlTaskQueueOperationsConsistencyTests : BaseTestWithConfiguration
	{
		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueConsumerOptions mConsumerOptions;

		private TaskQueueOptions mProducerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueOperationsConsistencyTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );
			mProducerOptions = TestOptions
				.GetDefaultTaskQueueProducerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mInfoOptions.ConnectionOptions.ConnectionString,
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
		public async Task Test_PeekMatchesDequeuedItem_SingleConsumer ()
		{
			IQueuedTask peekTask = null;
			IQueuedTaskToken dequeuedTaskToken = null;

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => mDataSource.LastPostedAt );

			using ( PostgreSqlTaskQueueConsumer taskQueue =
				CreateTaskQueueConsumer( () => mDataSource.LastPostedAt ) )
			{
				int expectedDequeueCount = mDataSource
					.NumTasksInQueue;

				for ( int i = 0; i < expectedDequeueCount; i++ )
				{
					peekTask = await taskQueueInfo.PeekAsync();
					Assert.NotNull( peekTask );

					dequeuedTaskToken = await taskQueue.DequeueAsync();
					Assert.NotNull( dequeuedTaskToken );

					Assert.AreEqual( peekTask.Id, dequeuedTaskToken
						.DequeuedTask
						.Id );
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_DequeueChangesPeekResult_SingleConsumer ()
		{
			IQueuedTask peekTask = null,
				rePeekTask = null;

			IQueuedTaskToken dequeuedTaskToken = null;

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => mDataSource.LastPostedAt );

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer( () => mDataSource.LastPostedAt ) )
			{
				peekTask = await taskQueueInfo.PeekAsync();
				Assert.NotNull( peekTask );

				dequeuedTaskToken = await taskQueueConsumer.DequeueAsync();
				Assert.NotNull( dequeuedTaskToken );

				rePeekTask = await taskQueueInfo.PeekAsync();
				Assert.NotNull( rePeekTask );

				//Removing a new element from the queue 
				//  occurs at the beginning of the queue,
				//  so peeking must yield a different result
				//  than before dequeue-ing
				Assert.AreNotEqual( rePeekTask.Id,
					peekTask.Id );
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

		[Test]
		[Repeat( 5 )]
		public async Task Test_StatsAreUpdatedCorrectly_AfterEnqueue_NewTask ()
		{
			Faker faker =
				new Faker();

			AbstractTimestamp postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => postedAt );

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer( () => postedAt ) )
			using ( TaskQueueMetricsDiffChecker diff = new TaskQueueMetricsDiffChecker( async ()
				=> await taskQueueInfo.ComputeMetricsAsync() ) )
			{
				await taskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				//Capture previous metrics
				await diff.CaptureInitialMetricsAsync();

				await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: nameof( Test_StatsAreUpdatedCorrectly_AfterEnqueue_NewTask ),
					priority: faker.Random.Int( 1, 100 ) );

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
		public async Task Test_StatsAreUpdatedCorrectly_AfterEnqueue_RepostExistingTask ()
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
				using ( TaskQueueMetricsDiffChecker diff = new TaskQueueMetricsDiffChecker( async ()
					=> await taskQueueInfo.ComputeMetricsAsync() ) )
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
						Source = nameof( Test_StatsAreUpdatedCorrectly_AfterEnqueue_RepostExistingTask ),
						Type = token.DequeuedTask.Type,
						LockedUntil = postedAt.Ticks + faker.Random.Long( 10, 1000 )
					};

					//Remove task record from DB - only dequeued tasks get reposted
					await mDataSource.RemoveQueuedTaskFromDbByIdAsync( token
						.DequeuedTask
						.Id );

					await taskQueueProducer.EnqueueAsync( repostTaskInfo );

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
		public async Task Test_StatsAreCorrectlyUpdated_AfterDequeue_NoTaskType ()
		{
			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => mDataSource.LastPostedAt );

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer( () => mDataSource.LastPostedAt ) )
			using ( TaskQueueMetricsDiffChecker diff = new TaskQueueMetricsDiffChecker( async ()
				=> await taskQueueInfo.ComputeMetricsAsync() ) )
			{
				await diff.CaptureInitialMetricsAsync();

				IQueuedTaskToken dequeuedToken = await taskQueueConsumer
					.DequeueAsync();

				QueuedTaskStatus origStatus = mDataSource
					.GetOriginalTokenData( dequeuedToken.DequeuedTask.Id )
						.LastQueuedTaskResult
						.Status;

				await diff.CaptureNewMetricsAndAssertCorrectDiff( delta: new TaskQueueMetrics(
					totalUnprocessed: origStatus == QueuedTaskStatus.Unprocessed ? -1 : 0,
					totalProcessing: 1,
					totalErrored: origStatus == QueuedTaskStatus.Error ? -1 : 0,
					totalFaulted: origStatus == QueuedTaskStatus.Faulted ? -1 : 0,
					totalFataled: origStatus == QueuedTaskStatus.Fatal ? -1 : 0,
					totalProcessed: origStatus == QueuedTaskStatus.Processed ? -1 : 0 ) );
			}
		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
