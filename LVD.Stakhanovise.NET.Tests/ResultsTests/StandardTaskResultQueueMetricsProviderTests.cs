using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.ResultsTests
{
	[TestFixture]
	public class StandardTaskResultQueueMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardTaskResultQueueMetricsProvider provider =
				new StandardTaskResultQueueMetricsProvider();

			AssertInitialMetricValues
				.WithExpectedCount( 6 )
				.Check( provider );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementPostResultCount( int times )
		{
			StandardTaskResultQueueMetricsProvider provider =
				new StandardTaskResultQueueMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementPostResultCount();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.ResultQueueResultPostCount,
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
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementResultWriteRequestTimeoutCount( int times )
		{
			StandardTaskResultQueueMetricsProvider provider =
				new StandardTaskResultQueueMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementResultWriteRequestTimeoutCount();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.ResultQueueResultWriteRequestTimeoutCount,
				times );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 10000 )]
		[Repeat( 10 )]
		public void Test_CanIncrementResultWriteCount( long millisecondDuration )
		{
			StandardTaskResultQueueMetricsProvider provider =
				new StandardTaskResultQueueMetricsProvider();

			TimeSpan duration = TimeSpan
				.FromMilliseconds( millisecondDuration );

			provider.IncrementResultWriteCount( duration );

			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueResultWriteCount,
				1 );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueTotalResultWriteDuration,
				millisecondDuration );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueMinimumResultWriteDuration,
				millisecondDuration );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueMaximumResultWriteDuration,
				millisecondDuration );

			long newMillisecondDuration = millisecondDuration 
				* 10;
			TimeSpan newDuration = TimeSpan
				.FromMilliseconds( newMillisecondDuration );

			provider.IncrementResultWriteCount( newDuration );

			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueResultWriteCount,
				2 );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueTotalResultWriteDuration,
				millisecondDuration + newMillisecondDuration );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueMinimumResultWriteDuration,
				millisecondDuration );
			DoAssertMetricValue( provider,
				AppMetricId.ResultQueueMaximumResultWriteDuration,
				newMillisecondDuration );
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
