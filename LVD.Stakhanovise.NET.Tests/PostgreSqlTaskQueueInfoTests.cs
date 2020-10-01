using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: also test dequeue with tasks still being locked
	[TestFixture]
	public class PostgreSqlTaskQueueInfoTests : BaseTestWithConfiguration
	{
		private TaskQueueInfoOptions mInfoOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueInfoTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
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
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public async Task Test_CanPeek ( int futureTicks )
		{
			IQueuedTask actualTopOfQueue;
			IQueuedTask expectedTopOfQueue = ExpectedTopOfQueueTask;

			for ( int i = 0; i <= futureTicks; i++ )
			{
				PostgreSqlTaskQueueInfo taskQueue = CreateTaskQueue( () => mDataSource
					.LastPostedAt
					.AddTicks( futureTicks ) );

				actualTopOfQueue = await taskQueue.PeekAsync();

				Assert.NotNull( actualTopOfQueue );
				Assert.AreEqual( expectedTopOfQueue.Id,
					actualTopOfQueue.Id );
			}
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanComputeQueueMetrics ( int futureTicks )
		{
			for ( int i = 0; i <= futureTicks; i++ )
			{
				PostgreSqlTaskQueueInfo taskQueue = CreateTaskQueue( () => mDataSource
					.LastPostedAt
					.AddTicks( futureTicks ) );

				TaskQueueMetrics metrics = await taskQueue
					.ComputeMetricsAsync();

				Assert.NotNull( metrics );
				Assert.AreEqual( mDataSource.NumUnProcessedTasks, metrics.TotalUnprocessed );
				Assert.AreEqual( mDataSource.NumErroredTasks, metrics.TotalErrored );
				Assert.AreEqual( mDataSource.NumFaultedTasks, metrics.TotalFaulted );
				Assert.AreEqual( mDataSource.NumFatalTasks, metrics.TotalFataled );
				Assert.AreEqual( mDataSource.NumProcessedTasks, metrics.TotalProcessed );
			}

		}

		private PostgreSqlTaskQueueInfo CreateTaskQueue ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private IQueuedTask ExpectedTopOfQueueTask
			=> mDataSource.SeededTasks.Where( t => mInfoOptions.ProcessWithStatuses
				.Contains( t.Status ) )
				.OrderByDescending( t => t.Priority )
				.OrderBy( t => t.PostedAt )
				.OrderBy( t => t.LockHandleId )
				.FirstOrDefault();

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
