using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Asserts;
using LVD.Stakhanovise.NET.Model;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.BufferTests
{
	[TestFixture]
	public class StandardTaskBufferMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardTaskBufferMetricsProvider provider =
				new StandardTaskBufferMetricsProvider();

			AssertInitialMetricValues
				.WithExpectedCount( 4 )
				.Check( provider );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementTimesEmptied( int times )
		{
			StandardTaskBufferMetricsProvider provider =
				new StandardTaskBufferMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementTimesEmptied();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.BufferTimesEmptied,
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
		public void Test_CanIncrementTimesFilled( int times )
		{
			StandardTaskBufferMetricsProvider provider =
				new StandardTaskBufferMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementTimesFilled();

			DoAssertMetricValueAndAllOthersDefault( provider,
				AppMetricId.BufferTimesFilled,
				times );
		}

		[Test]
		[TestCase( 10, 100 )]
		[Repeat( 10 )]
		public void Test_CanUpdateBufferCountStats( int minCount, int maxCount )
		{
			StandardTaskBufferMetricsProvider provider =
				new StandardTaskBufferMetricsProvider();

			provider.UpdateBufferCountStats( minCount );

			DoAssertMetricValue( provider,
				AppMetricId.BufferMinCount,
				minCount );

			DoAssertMetricValue( provider,
				AppMetricId.BufferMaxCount,
				minCount );

			provider.UpdateBufferCountStats( maxCount );

			DoAssertMetricValue( provider,
				AppMetricId.BufferMinCount,
				minCount );

			DoAssertMetricValue( provider,
				AppMetricId.BufferMaxCount,
				maxCount );
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
