using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertMetricValue
	{
		private object mExpectedValue;

		private AssertMetricValue( object expectedValue )
		{
			mExpectedValue = expectedValue;
		}

		public static AssertMetricValue For( object expectedValue )
		{
			return new AssertMetricValue( expectedValue );
		}

		public void Check( IAppMetricsProvider provider, AppMetricId metricId )
		{
			AppMetric queriedMetric = provider
				.QueryMetric( metricId );

			Assert.NotNull( queriedMetric );
			Assert.AreEqual( mExpectedValue,
				queriedMetric.Value );
		}
	}
}
