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
				CreateTaskQueueInfo();

			AbstractTimestamp now = new AbstractTimestamp( mDataSource.LastPostedAtTimeTick,
				mDataSource.LastPostedAtTimeTick * 1000 );

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueueConsumer() )
			{
				try
				{
					peekTask = await taskQueueInfo.PeekAsync( now );
					Assert.NotNull( peekTask );

					dequeuedTaskToken = await taskQueue.DequeueAsync( now );
					Assert.NotNull( dequeuedTaskToken );

					Assert.AreEqual( peekTask.Id,
						dequeuedTaskToken.QueuedTask.Id );
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
				CreateTaskQueueInfo();

			AbstractTimestamp now = new AbstractTimestamp( mDataSource.LastPostedAtTimeTick,
				mDataSource.LastPostedAtTimeTick * 1000 );

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer = CreateTaskQueueConsumer() )
			{
				peekTask = await taskQueueInfo.PeekAsync( now );
				Assert.NotNull( peekTask );

				dequeuedTaskToken = await taskQueueConsumer.DequeueAsync( now );
				Assert.NotNull( dequeuedTaskToken );

				rePeekTask = await taskQueueInfo.PeekAsync( now );
				Assert.NotNull( rePeekTask );

				//Removing a new element from the queue 
				//  occurs at the beginning of the queue,
				//  so peeking must yield a different result
				//  than before dequeue-ing
				Assert.AreNotEqual( rePeekTask.Id, 
					peekTask.Id );
			}
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ()
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ()
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
