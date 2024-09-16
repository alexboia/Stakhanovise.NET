using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertMetricValueAndAllOthersDefault
	{
		private object mExpectedValue;

		private AssertMetricValueAndAllOthersDefault( object expectedValue )
		{
			mExpectedValue = expectedValue;
		}

		public static AssertMetricValueAndAllOthersDefault For( object expectedValue )
		{
			return new AssertMetricValueAndAllOthersDefault( expectedValue );
		}

		public void Check( IAppMetricsProvider provider, AppMetricId metricId )
		{
			IEnumerable<AppMetric> metrics = provider
				.CollectMetrics();

			foreach ( AppMetric metric in metrics )
			{
				if ( metric.Id.Equals( metricId ) )
					ClassicAssert.AreEqual( mExpectedValue, metric.Value );
				else
					ClassicAssert.AreEqual( metric.Id.DefaultValue, metric.Value );
			}

			AppMetric queriedMetric = provider
				.QueryMetric( metricId );

			ClassicAssert.NotNull( queriedMetric );
			ClassicAssert.AreEqual( mExpectedValue,
				queriedMetric.Value );
		}
	}
}
