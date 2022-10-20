using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutionMetricsProvider : ITaskExecutionMetricsProvider
	{
		private readonly AppMetricsCollection mMetrics = new AppMetricsCollection
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

		public void UpdateTaskProcessingStats( TaskProcessingResult processingResult )
		{
			if ( processingResult == null )
				throw new ArgumentNullException( nameof( processingResult ) );
			
			mMetrics.UpdateMetric( AppMetricId.WorkerProcessedPayloadCount,
				m => m.Increment() );

			mMetrics.UpdateMetric( AppMetricId.WorkerTotalProcessingTime,
				m => m.Add( processingResult.ProcessingTimeMilliseconds ) );

			if ( processingResult.ExecutedSuccessfully )
				mMetrics.UpdateMetric( AppMetricId.WorkerSuccessfulProcessedPayloadCount,
					m => m.Increment() );

			else if ( processingResult.ExecutionFailed )
				mMetrics.UpdateMetric( AppMetricId.WorkerFailedProcessedPayloadCount,
					m => m.Increment() );

			else if ( processingResult.ExecutionCancelled )
				mMetrics.UpdateMetric( AppMetricId.WorkerProcessingCancelledPayloadCount,
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
