using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class StandardTaskResultQueueMetricsProvider : ITaskResultQueueMetricsProvider
	{
		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.ResultQueueMinimumResultWriteDuration, long.MaxValue ),
			new AppMetric( AppMetricId.ResultQueueMaximumResultWriteDuration, long.MinValue ),
			new AppMetric( AppMetricId.ResultQueueResultPostCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueResultWriteCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueResultWriteRequestTimeoutCount, 0 ),
			new AppMetric( AppMetricId.ResultQueueTotalResultWriteDuration, 0 )
		);

		public void IncrementPostResultCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultPostCount,
				m => m.Increment() );
		}

		public void IncrementResultWriteCount( TimeSpan duration )
		{
			long durationMilliseconds = ( long ) Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultWriteCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueTotalResultWriteDuration,
				m => m.Add( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueMinimumResultWriteDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.ResultQueueMaximumResultWriteDuration,
				m => m.Max( durationMilliseconds ) );
		}

		public void IncrementResultWriteRequestTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ResultQueueResultWriteRequestTimeoutCount,
				m => m.Increment() );
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
