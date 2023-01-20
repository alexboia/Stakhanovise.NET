using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.ExecutionPerformanceMonitorTests
{
	[TestFixture]
	public class StandardExecutionPerformanceMonitorMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardExecutionPerformanceMonitorMetricsProvider provider =
				new StandardExecutionPerformanceMonitorMetricsProvider();

			AssertInitialMetricValues
				.WithExpectedCount( 5 )
				.Check( provider );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementPerfMonPostCount( int times )
		{
			StandardExecutionPerformanceMonitorMetricsProvider provider =
				new StandardExecutionPerformanceMonitorMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementPerfMonPostCount();

			DoAssertMetricValue( provider, 
				AppMetricId.PerfMonReportPostCount, 
				times );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementPerfMonWriteRequestTimeoutCount(int times)
		{
			StandardExecutionPerformanceMonitorMetricsProvider provider =
				new StandardExecutionPerformanceMonitorMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementPerfMonWriteRequestTimeoutCount();

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonReportWriteRequestsTimeoutCount,
				times );
		}

		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 15 )]
		[TestCase( 1, 250 )]
		[TestCase( 10, 0 )]
		[TestCase( 10, 15 )]
		[TestCase( 10, 250 )]
		[Repeat( 10 )]
		public void Test_CanIncrementPerfMonWriteCount_Repeatedly_SameDuration( int times, int durationMilliseconds )
		{
			StandardExecutionPerformanceMonitorMetricsProvider provider =
				new StandardExecutionPerformanceMonitorMetricsProvider();

			TimeSpan duration = TimeSpan
				.FromMilliseconds( durationMilliseconds );

			for ( int i = 0; i < times; i++ )
				provider.IncrementPerfMonWriteCount( duration );

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonReportWriteCount,
				times );

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonMinimumReportWriteDuration,
				durationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonMaximumReportWriteDuration,
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
		public void Test_CanIncrementPerfMonWriteCount_Repeatedly_IncreasingDuration( int times, int initialDurationMilliseconds )
		{
			StandardExecutionPerformanceMonitorMetricsProvider provider =
				new StandardExecutionPerformanceMonitorMetricsProvider();

			for ( int i = 0; i < times; i++ )
			{
				int iterationDurationMilliseconds = initialDurationMilliseconds * ( i + 1 );
				TimeSpan duration = TimeSpan.FromMilliseconds( iterationDurationMilliseconds );
				
				provider.IncrementPerfMonWriteCount( duration );
			}

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonReportWriteCount,
				times );

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonMinimumReportWriteDuration,
				initialDurationMilliseconds );

			DoAssertMetricValue( provider,
				AppMetricId.PerfMonMaximumReportWriteDuration,
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
