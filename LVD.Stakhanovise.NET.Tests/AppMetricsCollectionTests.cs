using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using System.Linq;
using Bogus;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class AppMetricsCollectionTests
	{
		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetricIds_SpecificAppIds ()
		{
			AppMetricId[] expectedMetricIds = new AppMetricId[]
			{
				AppMetricId.BufferMaxCount,
				AppMetricId.BufferMinCount,
				AppMetricId.BufferTimesEmptied,
				AppMetricId.BufferTimesFilled,
				AppMetricId.ListenerNotificationWaitTimeoutCount,
				AppMetricId.ListenerReconnectCount,
				AppMetricId.ListenerTaskNotificationCount
			};

			AppMetricsCollection metrics = new AppMetricsCollection( expectedMetricIds );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				metrics );
		}

		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetricIds_AllBuiltInAppIds ()
		{
			AppMetricId[] expectedMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			AppMetricsCollection metrics = new AppMetricsCollection( expectedMetricIds );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				metrics );
		}

		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetrics ()
		{
			int iMetric = 0;
			Faker faker = new Faker();
			AppMetric[] expecteMetrics = new AppMetric[ AppMetricId.BuiltInAppMetricIds.Count() ];

			foreach ( AppMetricId mId in AppMetricId.BuiltInAppMetricIds )
				expecteMetrics[ iMetric++ ] = new AppMetric( mId, faker.Random.Long( 0 ) );

			AppMetricsCollection metrics = new AppMetricsCollection( expecteMetrics );

			Assert_AppMetricsCollection_CorrectlyInitialized( expecteMetrics, metrics );
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetricId> expectedMetricIds, AppMetricsCollection metrics )
		{
			IEnumerable<AppMetricId> actualMetricIds = metrics
				.ExportedMetrics;

			Assert.AreEqual( expectedMetricIds.Count(),
				actualMetricIds.Count() );

			foreach ( AppMetricId metricId in expectedMetricIds )
				CollectionAssert.Contains( actualMetricIds, metricId );

			foreach ( AppMetricId id in expectedMetricIds )
			{
				AppMetric metric = metrics.QueryMetric( id );
				Assert.NotNull( metric );
				Assert.AreEqual( 0, metric.Value );
			}

			Assert.AreEqual( expectedMetricIds.Count(), metrics
				.CollectMetrics()
				.Count() );

			foreach ( AppMetric metric in metrics.CollectMetrics() )
				Assert.AreEqual( 0, metric.Value );
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetric> expectedMetrics, AppMetricsCollection metrics )
		{
			IEnumerable<AppMetricId> actualMetricIds = metrics
				.ExportedMetrics;

			Assert.AreEqual( expectedMetrics.Count(),
				actualMetricIds.Count() );

			foreach ( AppMetric expectedMetric in expectedMetrics )
				CollectionAssert.Contains( actualMetricIds, expectedMetric.Id );

			foreach ( AppMetric m in expectedMetrics )
			{
				AppMetric metric = metrics.QueryMetric( m.Id );
				Assert.NotNull( metric );
				Assert.AreEqual( m.Value, metric.Value );
			}

			Assert.AreEqual( expectedMetrics.Count(), metrics
				.CollectMetrics()
				.Count() );
		}
	}
}
