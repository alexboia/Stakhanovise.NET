using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueOperationsConsistencyTests : BaseTestWithConfiguration
	{
		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueOperationsConsistencyTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

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
				try
				{
					peekTask = await taskQueueInfo.PeekAsync();
					Assert.NotNull( peekTask );

					dequeuedTaskToken = await taskQueue.DequeueAsync();
					Assert.NotNull( dequeuedTaskToken );

					Assert.AreEqual( peekTask.Id,
						dequeuedTaskToken.DequeuedTask.Id );
				}
				finally
				{
					await dequeuedTaskToken?.ReleaseLockAsync();
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
