using Bogus;
using LVD.Stakhanovise.NET.Model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockAppMetricsProvider : IAppMetricsProvider
	{
		private int mCollectMetricsCallCount;

		private readonly ConcurrentDictionary<IAppMetricId, int> mAppMetricIdQueryCount =
			new ConcurrentDictionary<IAppMetricId, int>();

		private readonly Dictionary<IAppMetricId, AppMetric> mMetricValues =
			new Dictionary<IAppMetricId, AppMetric>();

		public MockAppMetricsProvider( params IAppMetricId [] metricIds )
		{
			foreach ( IAppMetricId metricId in metricIds )
				mMetricValues [ metricId ] = new AppMetric( metricId, GeneratRandomValue() );
		}

		private long GeneratRandomValue()
		{
			return new Faker().Random.Long( 0, long.MaxValue / 2 );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			Interlocked.Increment( ref mCollectMetricsCallCount );
			return mMetricValues.Values;
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			mAppMetricIdQueryCount.AddOrUpdate( metricId, 1, ( mId, eValue ) => eValue + 1 );
			if ( !mMetricValues.TryGetValue( metricId, out AppMetric metricValue ) )
				metricValue = null;
			return metricValue;
		}

		public IEnumerable<IAppMetricId> ExportedMetrics 
			=> mMetricValues.Keys;

		public int CollectMetricsCallCount
			=> mCollectMetricsCallCount;
	}
}
