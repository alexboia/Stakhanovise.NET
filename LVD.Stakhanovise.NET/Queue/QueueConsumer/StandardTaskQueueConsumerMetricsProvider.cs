using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Queue
{
	public class StandardTaskQueueConsumerMetricsProvider : ITaskQueueConsumerMetricsProvider
	{
		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.QueueConsumerDequeueCount, 0 ),
			new AppMetric( AppMetricId.QueueConsumerMaximumDequeueDuration, long.MinValue ),
			new AppMetric( AppMetricId.QueueConsumerMinimumDequeueDuration, long.MaxValue ),
			new AppMetric( AppMetricId.QueueConsumerTotalDequeueDuration, 0 )
		);

		public void IncrementDequeueCount( TimeSpan duration )
		{
			long durationMilliseconds = ( long ) Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerDequeueCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerTotalDequeueDuration,
				m => m.Add( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerMinimumDequeueDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.QueueConsumerMaximumDequeueDuration,
				m => m.Max( durationMilliseconds ) );
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public IEnumerable<IAppMetricId> ExportedMetrics 
			=> mMetrics.ExportedMetrics;
	}
}
