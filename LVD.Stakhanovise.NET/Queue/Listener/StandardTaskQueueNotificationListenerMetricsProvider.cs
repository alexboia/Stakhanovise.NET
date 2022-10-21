using LVD.Stakhanovise.NET.Model;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Queue
{
	public class StandardTaskQueueNotificationListenerMetricsProvider : ITaskQueueNotificationListenerMetricsProvider
	{
		private readonly AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.ListenerTaskNotificationCount,
			AppMetricId.ListenerReconnectCount,
			AppMetricId.ListenerNotificationWaitTimeoutCount
		);

		public void IncrementNotificationWaitTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerNotificationWaitTimeoutCount,
				m => m.Increment() );
		}

		public void IncrementReconnectCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerReconnectCount,
				m => m.Increment() );
		}

		public void IncrementTaskNotificationCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerTaskNotificationCount,
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
