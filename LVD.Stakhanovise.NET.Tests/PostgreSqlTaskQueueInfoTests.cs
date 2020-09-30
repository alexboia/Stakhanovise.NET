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
			mInfoOptions = TestOptions.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mDataSource = new PostgreSqlTaskQueueDataSource( mInfoOptions.ConnectionOptions.ConnectionString,
				mInfoOptions.Mapping,
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
		[Repeat( 10 )]
		public async Task Test_CanPeek ()
		{
			IQueuedTask actualTopOfQueue;
			IQueuedTask expectedTopOfQueue = ExpectedTopOfQueueTask;

			AbstractTimestamp now = new AbstractTimestamp( mDataSource.LastPostedAtTimeTick,
				mDataSource.LastPostedAtTimeTick * 1000 );

			PostgreSqlTaskQueueInfo taskQueue =
				CreateTaskQueue();

			actualTopOfQueue = await taskQueue.PeekAsync( now );

			Assert.NotNull( actualTopOfQueue );
			Assert.AreEqual( expectedTopOfQueue.Id, actualTopOfQueue.Id );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanComputeQueueMetrics ()
		{
			PostgreSqlTaskQueueInfo taskQueue = CreateTaskQueue();

			//TODO: also add processing and processed tasks
			TaskQueueMetrics metrics = await taskQueue
				.ComputeMetricsAsync();

			Assert.NotNull( metrics );
			Assert.AreEqual( mDataSource.NumUnProcessedTasks, metrics.TotalUnprocessed );
			Assert.AreEqual( mDataSource.NumErroredTasks, metrics.TotalErrored );
			Assert.AreEqual( mDataSource.NumFaultedTasks, metrics.TotalFaulted );
			Assert.AreEqual( mDataSource.NumFatalTasks, metrics.TotalFataled );

		}

		private PostgreSqlTaskQueueInfo CreateTaskQueue ()
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions );
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
