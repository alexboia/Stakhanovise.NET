using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardExecutionPerformanceMonitorMetricsProvider : IExecutionPerformanceMonitorMetricsProvider
	{
		private readonly AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			new AppMetric( AppMetricId.PerfMonReportPostCount, 0 ),
			new AppMetric( AppMetricId.PerfMonReportWriteCount, 0 ),
			new AppMetric( AppMetricId.PerfMonMinimumReportWriteDuration, long.MaxValue ),
			new AppMetric( AppMetricId.PerfMonMaximumReportWriteDuration, long.MinValue ),
			new AppMetric( AppMetricId.PerfMonReportWriteRequestsTimeoutCount, 0 )
		);

		public void IncrementPerfMonPostCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PerfMonReportPostCount,
				m => m.Increment() );
		}

		public void IncrementPerfMonWriteCount( TimeSpan duration )
		{
			long durationMilliseconds = ( long ) Math.Ceiling( duration
				.TotalMilliseconds );

			mMetrics.UpdateMetric( AppMetricId.PerfMonReportWriteCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.PerfMonMinimumReportWriteDuration,
				m => m.Min( durationMilliseconds ) );

			mMetrics.UpdateMetric( AppMetricId.PerfMonMaximumReportWriteDuration,
				m => m.Max( durationMilliseconds ) );
		}

		public void IncrementPerfMonWriteRequestTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.PerfMonReportWriteRequestsTimeoutCount,
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
