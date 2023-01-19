using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Asserts;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Tests.PollerTests
{
	[TestFixture]
	public class StandardTaskPollerMetricsProviderTests
	{
		[Test]
		public void Test_InitialMetrics()
		{
			StandardTaskPollerMetricsProvider provider =
				new StandardTaskPollerMetricsProvider();

			AssertInitialMetricValues
				.WithExpectedCount( 4 )
				.Check( provider );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrementPollerDequeueCount( int times )
		{
			StandardTaskPollerMetricsProvider provider =
				new StandardTaskPollerMetricsProvider();

			for ( int i = 0; i < times; i++ )
				provider.IncrementPollerDequeueCount();

			DoAssertMetricValueAndAllOthersDefault( provider, 
				AppMetricId.PollerDequeueCount, 
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
		public void Test_CanIncrementPollerReturnedTaskCount()
		{

		}

		[Test]
		public void Test_CanIncrementPollerWaitForBufferSpaceCount()
		{

		}

		[Test]
		public void Test_CanIncrementPollerWaitForDequeueCount()
		{

		}


	}
}
