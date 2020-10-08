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
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

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

			AbstractTimestamp postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => postedAt );

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
				notificationWaitHandle.Set();

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer = CreateTaskQueueConsumer( () => postedAt ) )
			using ( TaskQueueMetricsDiffChecker diff = new TaskQueueMetricsDiffChecker( async () => await taskQueueInfo.ComputeMetricsAsync() ) )
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
				using ( TaskQueueMetricsDiffChecker diff = new TaskQueueMetricsDiffChecker( async () => await taskQueueInfo.ComputeMetricsAsync() ) )
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

					await mDataSource.RemoveQueuedTaskFromDbByIdAsync( token.DequeuedTask.Id );

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
				.GetQueuedTaskResultFromDbByIdAsync( queuedTask.Id );

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
