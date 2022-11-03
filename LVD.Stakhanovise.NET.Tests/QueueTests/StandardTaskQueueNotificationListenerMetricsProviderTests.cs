using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;

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

			AssertInitialMetricValues
				.WithExpectedCount( 3 )
				.Check( provider );
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

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.ListenerNotificationWaitTimeoutCount,
				times );
		}

		private void DoAssertMetricValueAndAllOthersDefault( IAppMetricsProvider provider,
			AppMetricId metricId,
			object expectedValue )
		{
			AssertMetricValueAndAllOthersDefault
				.For( expectedValue )
				.Check( provider, metricId );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_IncrementReconnectCount( int times )
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementReconnectCount();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.ListenerReconnectCount,
				times );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_IncrementTaskNotificationCount( int times )
		{
			StandardTaskQueueNotificationListenerMetricsProvider provider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementTaskNotificationCount();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.ListenerTaskNotificationCount,
				times );
		}
	}
}
