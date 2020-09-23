using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class ExecutionPerformanceMonitorWriteOptions
	{
		public ExecutionPerformanceMonitorWriteOptions ( int writeIntervalThreshold, 
			int writeCountThreshold )
		{
			WriteIntervalThresholdMilliseconds = writeIntervalThreshold;
			WriteCountThreshold = writeCountThreshold;
		}

		public int WriteIntervalThresholdMilliseconds { get; private set; }

		public int WriteCountThreshold { get; private set; }
	}
}
