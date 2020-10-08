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

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
				notificationWaitHandle.Set();

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer = CreateTaskQueueConsumer( () => postedAt ) )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				//Enqueue task and check result
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

			foreach ( IQueuedTaskToken token in mDataSource.CanBeRepostedSeededTaskTokens )
			{
				QueuedTaskInfo repostTaskInfo = new QueuedTaskInfo()
				{
					Id = token.DequeuedTask.Id,
					Priority = faker.Random.Int( 1, 100 ),
					Payload = token.DequeuedTask.Payload,
					Source = nameof( Test_CanEnqueue_RepostExistingTask ),
					Type = token.DequeuedTask.Type,
					LockedUntil = postedAt.Ticks + faker.Random.Long( 10, 1000 )
				};

				//Remove task record from DB - only dequeued tasks get reposted
				await mDataSource.RemoveQueuedTaskFromDbByIdAsync( token
					.DequeuedTask
					.Id );

				//Enqueue task and check result
				IQueuedTask requeuedTask = await taskQueueProducer
					.EnqueueAsync( repostTaskInfo );

				Assert.NotNull( requeuedTask );
				await Assert_ResultAddedOrUpdatedCorrectly( requeuedTask );
			}
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
