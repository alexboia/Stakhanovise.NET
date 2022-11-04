using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.AppMetricsTests
{
	[TestFixture]
	public class StandardAppMetricsMonitorTests
	{
		[Test]
		[TestCase( 100 )]
		[TestCase( 150 )]
		[TestCase( 250 )]
		[TestCase( 500 )]
		[Repeat( 25 )]
		public async Task Test_CanStartStop( int collectionIntervalMilliseconds )
		{
			MockAppMetricsMonitorWriter writer =
				CreateMockWriter( 1 );

			MockAppMetricsProvider metricsProvider =
				CreateMockAppMetricsProvider();

			AppMetricsMonitorOptions options =
				GetAppMetricsMonitorOptions( collectionIntervalMilliseconds );

			StandardAppMetricsMonitor monitor =
				new StandardAppMetricsMonitor( options,
					writer );

			await monitor.StartAsync( metricsProvider );
			Assert.IsTrue( monitor.IsRunning );

			await monitor.StopAsync();
			Assert.IsFalse( monitor.IsRunning );

			bool writeCountEventOccurred = writer.WaitForWriteCount( 1, collectionIntervalMilliseconds );
			Assert.IsTrue( writeCountEventOccurred );

			Assert.AreEqual( 1,
				metricsProvider.CollectMetricsCallCount );
			Assert.AreEqual( 1,
				writer.GetWrittenBatchCountForProcessId( options.ProcessId ) );
		}

		[Test]
		[TestCase( 1, 100 )]
		[TestCase( 1, 500 )]
		[TestCase( 1, 1000 )]
		[TestCase( 1, 1500 )]
		[TestCase( 3, 100 )]
		[TestCase( 3, 500 )]
		[TestCase( 3, 1000 )]
		[TestCase( 3, 1500 )]
		[Repeat( 10 )]
		public async Task Test_CanCollect( int stopAfterCycles, int collectionIntervalMilliseconds )
		{
			MockAppMetricsMonitorWriter writer =
				CreateMockWriter( stopAfterCycles );

			MockAppMetricsProvider metricsProvider =
				CreateMockAppMetricsProvider();

			AppMetricsMonitorOptions options =
				GetAppMetricsMonitorOptions( collectionIntervalMilliseconds );

			StandardAppMetricsMonitor monitor =
				new StandardAppMetricsMonitor( options,
					writer );

			await monitor.StartAsync( metricsProvider );

			bool writeCountEventOccurred = writer.WaitForWriteCount( stopAfterCycles, collectionIntervalMilliseconds );
			Assert.IsTrue( writeCountEventOccurred );

			writer.ResetWriteCountLock( 1 );
			await monitor.StopAsync();

			bool lastWriteCountEventOccurred = writer.WaitForWriteCount( 1,
				collectionIntervalMilliseconds );

			Assert.IsTrue( lastWriteCountEventOccurred );

			Assert.AreEqual( stopAfterCycles + 1,
				metricsProvider.CollectMetricsCallCount );
			Assert.AreEqual( stopAfterCycles + 1,
				writer.GetWrittenBatchCountForProcessId( options.ProcessId ) );
		}

		private MockAppMetricsProvider CreateMockAppMetricsProvider()
		{
			Faker faker = new Faker();
			AppMetricId [] pickIds = faker
				.PickRandom( AppMetricId.BuiltInAppMetricIds, AppMetricId.BuiltInAppMetricIds.Count() / 2 )
				.ToArray();

			return new MockAppMetricsProvider( pickIds );
		}

		private MockAppMetricsMonitorWriter CreateMockWriter( int unlockAfterWriteCount )
		{
			return new MockAppMetricsMonitorWriter( unlockAfterWriteCount );
		}

		private AppMetricsMonitorOptions GetAppMetricsMonitorOptions( int collectionIntervalMilliseconds )
		{
			return new AppMetricsMonitorOptions( Guid.NewGuid().ToString(),
				collectionIntervalMilliseconds );
		}
	}
}
