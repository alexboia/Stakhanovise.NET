using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StakhanoviseSetupDefaults
	{
		public Assembly[] ExecutorAssemblies { get; set; }

		public QueuedTaskStatus[] ProcessWithStatuses { get; set; }

		public int WorkerCount { get; set; }

		public int QueueConsumerConnectionPoolSize { get; set; }

		public QueuedTaskMapping Mapping { get; set; }

		public int AbstractTimeTickTimeoutMilliseconds { get; set; }

		public long DefaultEstimatedProcessingTimeMilliseconds { get; set; }

		public Func<int, long> CalculateDelayTicksTaskAfterFailure { get; set; }

		public Func<IQueuedTask, TaskExecutionStats, long> CalculateEstimatedProcessingTimeMilliseconds { get; set; }

		public Func<IQueuedTask, Exception, bool> IsTaskErrorRecoverable { get; set; }

		public bool ExecutionPerformanceMonitorFlushStats { get; set; }

		public int ExecutionPerformanceMonitorWriteCountThreshold { get; set; }

		public int ExecutionPerformanceMonitorWriteIntervalThresholdMilliseconds { get; set; }

		public int BuiltInTimingBeltInitialWallclockTimeCost { get; set; }

		public int BuiltInTimingBeltTimeTickBatchSize { get; set; }

		public int BuiltInTimingBeltTimeTickMaxFailCount { get; set; }
	}
}
