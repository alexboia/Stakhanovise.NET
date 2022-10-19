using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutionMetricsProvider : ITaskExecutionMetricsProvider
	{
		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.WorkerProcessedPayloadCount,
			AppMetricId.WorkerBufferWaitCount,
			AppMetricId.WorkerTotalProcessingTime,
			AppMetricId.WorkerSuccessfulProcessedPayloadCount,
			AppMetricId.WorkerFailedProcessedPayloadCount,
			AppMetricId.WorkerProcessingCancelledPayloadCount
		);

		public void IncrementBufferWaitCount()
		{
			mMetrics.UpdateMetric( AppMetricId.WorkerBufferWaitCount, 
				m => m.Increment() );
		}

		public void UpdateTaskProcessingStats( TaskExecutionResult result )
		{
			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );
			
			mMetrics.UpdateMetric( AppMetricId.WorkerProcessedPayloadCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.WorkerTotalProcessingTime,
				m => m.Add( result.ProcessingTimeMilliseconds ) );

			if ( result.ExecutedSuccessfully )
				mMetrics.UpdateMetric( AppMetricId.WorkerSuccessfulProcessedPayloadCount,
					m => m.Increment() );

			else if ( result.ExecutionFailed )
				mMetrics.UpdateMetric( AppMetricId.WorkerFailedProcessedPayloadCount,
					m => m.Increment() );

			else if ( result.ExecutionCancelled )
				mMetrics.UpdateMetric( AppMetricId.WorkerProcessingCancelledPayloadCount,
					m => m.Increment() );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public AppMetric QueryMetric( AppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetricId> ExportedMetrics 
			=> mMetrics.ExportedMetrics;
	}
}
