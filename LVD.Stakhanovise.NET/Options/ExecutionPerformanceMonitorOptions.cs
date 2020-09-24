using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class ExecutionPerformanceMonitorOptions
	{
		public ExecutionPerformanceMonitorOptions ()
		{
			FlushStats = true;
			FlushOptions = new ExecutionPerformanceMonitorWriteOptions( 1000, 10 );
		}

		public bool FlushStats { get; private set; }

		public ExecutionPerformanceMonitorWriteOptions FlushOptions { get; private set; }
	}
}
