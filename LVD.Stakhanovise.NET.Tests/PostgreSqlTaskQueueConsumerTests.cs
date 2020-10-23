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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class PostgreSqlTaskQueueConsumerTests : BaseDbTests
	{
		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueConsumerTests ()
		{
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mConsumerOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[SetUp]
		public async Task TestSetUp ()
		{
			await mDataSource.ClearData();
			await mDataSource.SeedData();
		}

		[TearDown]
		public async Task TestTearDown ()
		{
			await mDataSource.ClearData();
		}

		private async Task Run_ConsumeTestAsync ( PostgreSqlTaskQueueConsumer taskQueue,
			ConsumedQueuedTaskTokenChecker checker,
			DateTimeOffset refNow,
			params string[] payloadTypes )
		{
			IQueuedTaskToken newTaskToken;
			if ( payloadTypes != null && payloadTypes.Length > 0 )
				newTaskToken = await taskQueue.DequeueAsync( payloadTypes );
			else
				newTaskToken = await taskQueue.DequeueAsync();

			checker.AssertConsumedTokenValid( newTaskToken, refNow );

			await checker.AssertTaskNotInDbAnymoreAsync( newTaskToken );
			await checker.AssertTaskResultInDbAndCorrectAsync( newTaskToken );
		}

		[Test]
		[Repeat( 20 )]
		public async Task Test_CanDequeue_WithTaskTypes_OneTypePerDequeueCall ()
		{
			DateTimeOffset refNow = mDataSource
				.LastPostedAt
				.AddMilliseconds( 1 );

			using ( PostgreSqlTaskQueueConsumer taskQueue =
				CreateTaskQueue( () => refNow ) )
			using ( ConsumedQueuedTaskTokenChecker checker =
				new ConsumedQueuedTaskTokenChecker( mDataSource ) )
			{
				foreach ( Type taskType in mDataSource.InQueueTaskTypes )
				{
					int expectedDequeueCount = mDataSource.CountTasksOfTypeInQueue( taskType );
					for ( int i = 0; i < expectedDequeueCount; i++ )
						await Run_ConsumeTestAsync( taskQueue,
							checker,
							refNow,
							taskType.FullName );
				}
			}
		}

		[Test]
		[Repeat( 20 )]
		public async Task Test_CanDequeue_WithTaskTypes_MultipleTypesPerDequeueCall ()
		{
			DateTimeOffset refNow = mDataSource
				.LastPostedAt
				.AddMilliseconds( 1 );

			int expectedDequeueCount = mDataSource
				.NumTasksInQueue;

			string[] taskTypes = mDataSource.InQueueTaskTypes
				.Select( t => t.FullName )
				.ToArray();

			using ( PostgreSqlTaskQueueConsumer taskQueue =
				CreateTaskQueue( () => refNow ) )
			using ( ConsumedQueuedTaskTokenChecker checker =
				new ConsumedQueuedTaskTokenChecker( mDataSource ) )
			{
				for ( int i = 0; i < expectedDequeueCount; i++ )
					await Run_ConsumeTestAsync( taskQueue,
						checker,
						refNow,
						taskTypes );
			}
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 15 )]
		[Repeat( 20 )]
		public async Task Test_CanDequeue_WithTaskTypes_MultipleTypesPerDequeueCall_ParallelConsumers ( int nConsumers )
		{
			int expectedDequeueCount = mDataSource
				.NumTasksInQueue;

			DateTimeOffset refNow = mDataSource
				.LastPostedAt
				.AddMilliseconds( 1 );

			string[] taskTypes = mDataSource.InQueueTaskTypes
				.Select( t => t.FullName )
				.ToArray();

			Task[] consumers = new Task[ nConsumers ];

			int[] loopPartitions = expectedDequeueCount
				.PartitionValue( nConsumers );

			Assert.AreEqual( expectedDequeueCount,
				loopPartitions.Sum() );

			for ( int iConsumer = 0; iConsumer < nConsumers; iConsumer++ )
			{
				int loopCount = loopPartitions[ iConsumer ];
				consumers[ iConsumer ] = Task.Run( async () =>
				{
					using ( PostgreSqlTaskQueueConsumer taskQueue =
						CreateTaskQueue( () => refNow ) )
					using ( ConsumedQueuedTaskTokenChecker checker =
						new ConsumedQueuedTaskTokenChecker( mDataSource ) )
					{
						for ( int iTest = 0; iTest < loopCount; iTest++ )
							await Run_ConsumeTestAsync( taskQueue,
								checker,
								refNow,
								taskTypes );
					}
				} );
			}

			await Task.WhenAll( consumers );
		}

		[Test]
		[Repeat( 20 )]
		public async Task Test_CanDequeue_WithoutTaskTypes ()
		{
			DateTimeOffset refNow = mDataSource
				.LastPostedAt
				.AddMilliseconds( 1 );

			int expectedDequeueCount = mDataSource
				.NumTasksInQueue;

			using ( PostgreSqlTaskQueueConsumer taskQueue =
				CreateTaskQueue( () => refNow ) )
			using ( ConsumedQueuedTaskTokenChecker checker =
				new ConsumedQueuedTaskTokenChecker( mDataSource ) )
			{
				for ( int i = 0; i < expectedDequeueCount; i++ )
					await Run_ConsumeTestAsync( taskQueue, checker, refNow );
			}
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 15 )]
		[Repeat( 20 )]
		public async Task Test_CanDequeue_WithoutTaskTypes_ParallelConsumers ( int nConsumers )
		{
			int expectedDequeueCount = mDataSource
				.NumTasksInQueue;

			DateTimeOffset refNow = mDataSource
				.LastPostedAt
				.AddMilliseconds( 1 );

			Task[] consumers = new Task[ nConsumers ];

			int[] loopCountForConsumers = expectedDequeueCount
				.PartitionValue( nConsumers );

			Assert.AreEqual( expectedDequeueCount,
				loopCountForConsumers.Sum() );

			for ( int iConsumer = 0; iConsumer < nConsumers; iConsumer++ )
			{
				int loopCount = loopCountForConsumers[ iConsumer ];
				consumers[ iConsumer ] = Task.Run( async () =>
				{
					using ( PostgreSqlTaskQueueConsumer taskQueue =
						CreateTaskQueue( () => refNow ) )
					using ( ConsumedQueuedTaskTokenChecker checker =
						new ConsumedQueuedTaskTokenChecker( mDataSource ) )
					{
						for ( int iTest = 0; iTest < loopCount; iTest++ )
							await Run_ConsumeTestAsync( taskQueue,
								checker,
								refNow );
					}
				} );
			}

			await Task.WhenAll( consumers );
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanStartStopReceivingNewTaskNotificationUpdates ()
		{
			ManualResetEvent notificationWaitHandle = new
				ManualResetEvent( false );

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => mDataSource.LastPostedAt ) )
			{
				taskQueue.ClearForDequeue += ( s, e ) =>
					notificationWaitHandle.Set();

				await taskQueue.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await SendNewTaskNotificationAsync();
				notificationWaitHandle.WaitOne();

				await taskQueue.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		private async Task SendNewTaskNotificationAsync ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
				await db.NotifyAsync( mConsumerOptions.Mapping.NewTaskNotificationChannelName, null );
			await Task.Delay( 100 );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueue ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			return await OpenDbConnectionAsync( ConnectionString );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
