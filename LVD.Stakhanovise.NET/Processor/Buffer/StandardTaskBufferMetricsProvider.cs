using LVD.Stakhanovise.NET.Model;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskBufferMetricsProvider : ITaskBufferMetricsProvider
	{
		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.BufferMinCount, long.MaxValue ),
			new AppMetric( AppMetricId.BufferMaxCount, long.MinValue ),
			new AppMetric( AppMetricId.BufferTimesEmptied, 0 ),
			new AppMetric( AppMetricId.BufferTimesFilled, 0 )
		);

		public void IncrementTimesEmptied()
		{
			mMetrics.UpdateMetric( AppMetricId.BufferTimesEmptied,
				m => m.Increment() );
		}

		public void IncrementTimesFilled()
		{
			mMetrics.UpdateMetric( AppMetricId.BufferTimesFilled,
				m => m.Increment() );
		}

		public void UpdateBufferCountStats( int newCount )
		{
			mMetrics.UpdateMetric( AppMetricId.BufferMaxCount,
				m => m.Max( newCount ) );
			mMetrics.UpdateMetric( AppMetricId.BufferMinCount,
				m => m.Min( newCount ) );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<IAppMetricId> ExportedMetrics
			=> mMetrics.ExportedMetrics;
	}
}
