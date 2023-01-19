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

		private TaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueOperationsConsistencyTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );
			mProducerOptions = TestOptions
				.GetDefaultTaskQueueOptions( ConnectionString );

			mDataSource = new TaskQueueDataSource( mInfoOptions.ConnectionOptions.ConnectionString,
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

			DateTimeOffset postedAt = mDataSource.LastPostedAt
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

			DateTimeOffset postedAt = mDataSource.LastPostedAt
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

					QueuedTaskProduceInfo repostTaskInfo = new QueuedTaskProduceInfo()
					{
						Id = token.DequeuedTask.Id,
						Priority = faker.Random.Int( 1, 100 ),
						Payload = token.DequeuedTask.Payload,
						Source = nameof( Test_StatsAreUpdatedCorrectly_AfterEnqueue_RepostExistingTask ),
						Type = token.DequeuedTask.Type,
						LockedUntilTs = postedAt.AddMinutes( faker.Random.Long( 1000, 10000 ) )
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

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new StandardTaskQueueConsumerMetricsProvider(),
				new TaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
