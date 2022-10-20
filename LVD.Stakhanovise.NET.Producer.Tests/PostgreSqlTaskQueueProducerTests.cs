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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Producer.Tests.Asserts;
using LVD.Stakhanovise.NET.Producer.Tests.Support;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueProducerTests : BaseTestWithConfiguration
	{
		private TaskQueueOptions mProducerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueProducerTests()
		{
			mProducerOptions = CommonTestOptions
				.GetDefaultTaskQueueOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mProducerOptions.ConnectionString,
				mProducerOptions.Mapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[SetUp]
		public async Task TestSetUp()
		{
			await mDataSource.SeedData();
			await Task.Delay( 100 );
		}

		[TearDown]
		public async Task TestTearDown()
		{
			await mDataSource.ClearData();
			await Task.Delay( 100 );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue_NewTask_Serial()
		{
			DateTimeOffset lastPostedAt =
				GetLastPostedAt();

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => lastPostedAt );

			using ( NpgsqlNotificationMonitor monitor = new NpgsqlNotificationMonitor( mProducerOptions, 1 ) )
			{
				await monitor.BeginWatchNotificationsAsync();

				//Enqueue task and check result
				IQueuedTask queuedTask = await taskQueueProducer
					.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
						source: TaskSourceName,
						priority: RandomPriority() );

				await AssertResultAddedOrUpdatedCorrectly
					.LookIn( mDataSource )
					.CheckAsync( queuedTask );

				monitor.WaitForNotificationsToBeReceived();
				await monitor.EndWatchNotificationsAsync();
			}
		}

		private DateTimeOffset GetLastPostedAt()
		{
			return mDataSource
				.LastPostedAt
				.AddTicks( 1 );
		}

		private async Task<NpgsqlConnection> TryOpenConnectionAsync()
		{
			return await mProducerOptions
				.ConnectionOptions
				.TryOpenConnectionAsync();
		}

		private int RandomPriority()
		{
			return new Faker().Random
				.Int( 1, 100 );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public async Task Test_CanEnqueue_NewTask_ParallelProducers( int nProducers )
		{
			DateTimeOffset lastPostedAt =
				GetLastPostedAt();

			Task [] producerThreads =
				new Task [ nProducers ];

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => lastPostedAt );

			using ( NpgsqlNotificationMonitor monitor = new NpgsqlNotificationMonitor( mProducerOptions, nProducers ) )
			{
				await monitor.BeginWatchNotificationsAsync();

				for ( int i = 0; i < nProducers; i++ )
				{
					producerThreads [ i ] = Task.Run( async () =>
					{
						//Enqueue task and check result
						IQueuedTask queuedTask = await taskQueueProducer
							 .EnqueueAsync( payload: new SampleTaskPayload( 100 ),
								source: TaskSourceName,
								priority: RandomPriority() );

						await AssertResultAddedOrUpdatedCorrectly
							.LookIn( mDataSource )
							.CheckAsync( queuedTask );
					} );
				}

				monitor.WaitForNotificationsToBeReceived();
				await monitor.EndWatchNotificationsAsync();
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanEnqueue_RepostExistingTask_Serial()
		{
			DateTimeOffset lastPostedAt =
				GetLastPostedAt();

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => lastPostedAt );

			foreach ( IQueuedTaskToken token in mDataSource.CanBeRepostedSeededTaskTokens )
			{
				QueuedTaskProduceInfo repostTaskInfo =
					CreateProduceInfo( token,
						lastPostedAt );

				//Remove task record from DB - only dequeued tasks get reposted
				await mDataSource.RemoveQueuedTaskFromDbByIdAsync( token
					.DequeuedTask
					.Id );

				//Enqueue task and check result
				IQueuedTask requeuedTask = await taskQueueProducer
					.EnqueueAsync( repostTaskInfo );

				await AssertResultAddedOrUpdatedCorrectly
					.LookIn( mDataSource )
					.CheckAsync( requeuedTask );
			}
		}

		private QueuedTaskProduceInfo CreateProduceInfo( IQueuedTaskToken token,
			DateTimeOffset lastPostedAt )
		{
			return new QueuedTaskProduceInfo()
			{
				Id = token.DequeuedTask.Id,
				Priority = RandomPriority(),
				Payload = token.DequeuedTask.Payload,
				Source = TaskSourceName,
				Type = token.DequeuedTask.Type,
				LockedUntilTs = lastPostedAt
					.AddMilliseconds( RandomLockDuration() )
			};
		}

		private long RandomLockDuration()
		{
			return new Faker().Random
				.Long( 1000, 10000 );
		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TestTaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private string NewTaskNotificationChannelName
			=> mProducerOptions.Mapping.NewTaskNotificationChannelName;

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );

		private string TaskSourceName
			=> GetType().FullName;
	}
}
