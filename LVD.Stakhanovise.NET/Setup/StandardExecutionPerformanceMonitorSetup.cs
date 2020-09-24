using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardExecutionPerformanceMonitorSetup : IExecutionPerformanceMonitorSetup
	{
		private bool mFlushStats = true;

		private int mWriteCountThreshold = 10;

		private int mWriteIntervalThresholdMilliseconds = 1000;

		public IExecutionPerformanceMonitorSetup FlushStats ( bool enabled )
		{
			mFlushStats = enabled;
			return this;
		}

		public IExecutionPerformanceMonitorSetup WithWriteCountThreshold ( int writeCountThreshold )
		{
			mWriteCountThreshold = writeCountThreshold;
			return this;
		}

		public IExecutionPerformanceMonitorSetup WithWriteIntervalThresholdMilliseconds ( int writeIntervalThresholdMilliseconds )
		{
			mWriteIntervalThresholdMilliseconds = writeIntervalThresholdMilliseconds;
			return this;
		}

		public ExecutionPerformanceMonitorOptions BuildOptions ()
		{
			return new ExecutionPerformanceMonitorOptions( mFlushStats,
				new ExecutionPerformanceMonitorWriteOptions( mWriteIntervalThresholdMilliseconds,
					mWriteCountThreshold ) );
		}
	}
}
