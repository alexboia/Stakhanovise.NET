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
using System.Collections.Concurrent;
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
				.GetDefaultTaskQueueProducerAndResultOptions( ConnectionString );
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
		public async Task Test_CanEnqueue_NewTask_Serial ()
		{
			Faker faker =
				new Faker();

			ManualResetEvent notificationWaitHandle =
				new ManualResetEvent( false );

			DateTimeOffset postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
			{
				if ( e.Reason == ClearForDequeReason.NewTaskPostedNotificationReceived )
					notificationWaitHandle.Set();
			};

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer( () => postedAt ) )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer
					.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer
					.IsReceivingNewTaskUpdates );

				//Enqueue task and check result
				IQueuedTask queuedTask = await taskQueueProducer
					.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
						source: nameof( Test_CanEnqueue_NewTask_Serial ),
						priority: faker.Random.Int( 1, 100 ) );

				Assert.NotNull( queuedTask );
				await Assert_ResultAddedOrUpdatedCorrectly( queuedTask );

				notificationWaitHandle.WaitOne();

				await taskQueueConsumer
					.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueueConsumer
					.IsReceivingNewTaskUpdates );

				taskQueueConsumer.ClearForDequeue -=
					handleClearForDequeue;
			}
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public async Task Test_CanEnqueue_NewTask_ParallelProducers ( int nProducers )
		{
			Faker faker =
				new Faker();

			CountdownEvent notificationWaitHandle =
				new CountdownEvent( nProducers );

			DateTimeOffset postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
			{
				if ( e.Reason == ClearForDequeReason.NewTaskPostedNotificationReceived )
					notificationWaitHandle.Signal();
			};

			Task[] producers = new Task[ nProducers ];

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer( () => postedAt ) )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer
					.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer
					.IsReceivingNewTaskUpdates );

				for ( int i = 0; i < nProducers; i++ )
				{
					producers[ i ] = Task.Run( async () =>
					{
						//Enqueue task and check result
						IQueuedTask queuedTask = await taskQueueProducer
							.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
								 source: nameof( Test_CanEnqueue_NewTask_ParallelProducers ),
								priority: faker.Random.Int( 1, 100 ) );

						Assert.NotNull( queuedTask );
						await Assert_ResultAddedOrUpdatedCorrectly( queuedTask );
					} );
				}

				notificationWaitHandle.Wait();

				await taskQueueConsumer
					.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueueConsumer
					.IsReceivingNewTaskUpdates );

				taskQueueConsumer.ClearForDequeue -=
					handleClearForDequeue;
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue_RepostExistingTask_Serial ()
		{
			Faker faker =
				new Faker();

			DateTimeOffset postedAt = mDataSource.LastPostedAt
				.AddSeconds( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			foreach ( IQueuedTaskToken token in mDataSource.CanBeRepostedSeededTaskTokens )
			{
				QueuedTaskProduceInfo repostTaskInfo = new QueuedTaskProduceInfo()
				{
					Id = token.DequeuedTask.Id,
					Priority = faker.Random.Int( 1, 100 ),
					Payload = token.DequeuedTask.Payload,
					Source = nameof( Test_CanEnqueue_RepostExistingTask_Serial ),
					Type = token.DequeuedTask.Type,
					LockedUntilTs = postedAt.AddMilliseconds( faker.Random.Long( 1000, 10000 ) )
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
			Assert.LessOrEqual( Math.Abs( ( queuedTask.PostedAtTs - queuedTaskResult.PostedAtTs ).TotalMilliseconds ),
				10 );
			Assert.AreEqual( QueuedTaskStatus.Unprocessed,
				queuedTaskResult.Status );
		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TestTaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
