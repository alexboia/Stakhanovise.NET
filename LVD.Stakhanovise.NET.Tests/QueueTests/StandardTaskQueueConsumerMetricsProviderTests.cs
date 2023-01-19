using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	[TestFixture]
	public class StandardTaskQueueConsumerMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardTaskQueueConsumerMetricsProvider provider =
				new StandardTaskQueueConsumerMetricsProvider();

			AssertInitialMetricValues
				.WithExpectedCount( 4 )
				.Check( provider );
		}


		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 15 )]
		[TestCase( 1, 250 )]
		[TestCase( 10, 0 )]
		[TestCase( 10, 15 )]
		[TestCase( 10, 250 )]
		[Repeat( 10 )]
		public void Test_CanIncrementDequeueCount_Repeatedly_SameDuration( int times, int durationMilliseconds )
		{
			StandardTaskQueueConsumerMetricsProvider provider =
				new StandardTaskQueueConsumerMetricsProvider();

			TimeSpan duration = TimeSpan
				.FromMilliseconds( durationMilliseconds );

			for ( int i = 0; i < times; i++ )
				provider.IncrementDequeueCount( duration );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerDequeueCount,
				times );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerTotalDequeueDuration,
				times * durationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerMinimumDequeueDuration,
				durationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerMaximumDequeueDuration,
				durationMilliseconds );
		}

		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 15 )]
		[TestCase( 1, 250 )]
		[TestCase( 10, 0 )]
		[TestCase( 10, 15 )]
		[TestCase( 10, 250 )]
		[Repeat( 10 )]
		public void Test_CanIncrementDequeueCount_Repeatedly_IncreasingDuration( int times, int initialDurationMilliseconds )
		{
			int totalDurationMilliseconds = 0;
			StandardTaskQueueConsumerMetricsProvider provider =
				new StandardTaskQueueConsumerMetricsProvider();

			for ( int i = 0; i < times; i++ )
			{
				int iterationDurationMilliseconds = initialDurationMilliseconds * ( i + 1 );
				TimeSpan duration = TimeSpan.FromMilliseconds( iterationDurationMilliseconds );

				totalDurationMilliseconds += iterationDurationMilliseconds;
				provider.IncrementDequeueCount( duration );
			}

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerDequeueCount,
				times );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerTotalDequeueDuration,
				totalDurationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerMinimumDequeueDuration,
				initialDurationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.QueueConsumerMaximumDequeueDuration,
				initialDurationMilliseconds * times );
		}

		private void DoAssertMetricValue( IAppMetricsProvider provider,
			AppMetricId metricId,
			object expectedValue )
		{
			AssertMetricValue
				.For( expectedValue )
				.Check( provider, metricId );
		}
	}
}
