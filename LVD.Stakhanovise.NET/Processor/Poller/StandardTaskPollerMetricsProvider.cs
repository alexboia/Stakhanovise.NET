using LVD.Stakhanovise.NET.Model;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskPollerMetricsProvider : ITaskPollerMetricsProvider
	{
		private readonly AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.PollerDequeueCount,
			AppMetricId.PollerReturnedTaskCount,
			AppMetricId.PollerWaitForBufferSpaceCount,
			AppMetricId.PollerWaitForDequeueCount
		);

		public void IncrementPollerDequeueCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerDequeueCount,
				m => m.Increment() );
		}

		public void IncrementPollerReturnedTaskCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerReturnedTaskCount,
				m => m.Increment() );
		}

		public void IncrementPollerWaitForBufferSpaceCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerWaitForBufferSpaceCount,
				m => m.Increment() );
		}

		public void IncrementPollerWaitForDequeueCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PollerWaitForDequeueCount,
				m => m.Increment() );
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
