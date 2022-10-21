using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	[TestFixture]
	public class StandardTaskQueueNotificationListenerMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			IEnumerable<AppMetric> metrics = provider
				.CollectMetrics();

			Assert.AreEqual( 3, metrics.Count() );
			foreach ( AppMetric metric in metrics )
				Assert.AreEqual( 0, metric.Value );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementNotificationWaitTimeoutCount( int times )
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementNotificationWaitTimeoutCount();

			AssertMetricValue( provider,
				AppMetricId.ListenerNotificationWaitTimeoutCount,
				times );
		}

		private static void AssertMetricValue( StandardTaskQueueNotificationListenerMetricsProvider provider,
			AppMetricId metricId,
			object expectedValue )
		{
			IEnumerable<AppMetric> metrics = provider
				.CollectMetrics();

			Assert.AreEqual( 3, metrics.Count() );
			foreach ( AppMetric metric in metrics )
			{
				if ( metric.Id.Equals( metricId ) )
					Assert.AreEqual( expectedValue, metric.Value );
				else
					Assert.AreEqual( 0, metric.Value );
			}

			AppMetric waitTimeoutCountMetric = provider
				.QueryMetric( metricId );

			Assert.NotNull( waitTimeoutCountMetric );
			Assert.AreEqual( expectedValue, waitTimeoutCountMetric.Value );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_IncrementReconnectCount(int times)
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementReconnectCount();

			AssertMetricValue( provider,
				AppMetricId.ListenerReconnectCount,
				times );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_IncrementTaskNotificationCount(int times)
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementTaskNotificationCount();

			AssertMetricValue( provider,
				AppMetricId.ListenerTaskNotificationCount,
				times );
		}
	}
}
