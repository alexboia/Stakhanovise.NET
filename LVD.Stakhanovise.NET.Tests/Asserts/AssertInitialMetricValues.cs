using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertInitialMetricValues
	{
		private int mExpectedCount;

		private AssertInitialMetricValues( int expectedCount )
		{
			mExpectedCount = expectedCount;
		}

		public static AssertInitialMetricValues WithExpectedCount( int expectedCount )
		{
			return new AssertInitialMetricValues( expectedCount );
		}

		public void Check( IAppMetricsProvider provider )
		{
			IEnumerable<AppMetric> metrics = provider
				.CollectMetrics();

			Assert.AreEqual( mExpectedCount, metrics.Count() );
			foreach ( AppMetric metric in metrics )
				Assert.AreEqual( metric.Id.DefaultValue, metric.Value );
		}
	}
}
