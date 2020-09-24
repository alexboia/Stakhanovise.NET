using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class TaskProcessingOptions
	{
		public TaskProcessingOptions ()
		{
			AbstractTimeTickTimeoutMilliseconds = 1000;
			CalculateDelayTicksTaskAfterFailure = errorCount => ( long )Math.Pow( 100, errorCount );
			DefaultEstimatedProcessingTimeMilliseconds = 1000;
			CalculateEstimatedProcessingTimeMilliseconds = ( task, stats ) => stats.LongestExecutionTime > 0 
				? stats.LongestExecutionTime 
				: DefaultEstimatedProcessingTimeMilliseconds;

			IsTaskErrorRecoverable = ( task, exc ) => true;
		}

		public int AbstractTimeTickTimeoutMilliseconds { get; private set; }

		public Func<int, long> CalculateDelayTicksTaskAfterFailure { get; private set; }

		public long DefaultEstimatedProcessingTimeMilliseconds { get; private set; }

		public Func<IQueuedTask, TaskExecutionStats, long> CalculateEstimatedProcessingTimeMilliseconds { get; private set; }

		public Func<IQueuedTask, Exception, bool> IsTaskErrorRecoverable { get; private set; }
	}
}
