using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class TaskEngineOptions
	{
		public TaskEngineOptions ( ConnectionOptions generalQueueConnectionOptions )
		{
			if ( generalQueueConnectionOptions == null )
				throw new ArgumentNullException( nameof( generalQueueConnectionOptions ) );

			WorkerCount = Math.Max( 1, Environment.ProcessorCount - 1 );
			TaskProcessingOptions = new TaskProcessingOptions();
			PerfMonOptions = new ExecutionPerformanceMonitorOptions();
			TaskQueueOptions = new TaskQueueOptions( generalQueueConnectionOptions, WorkerCount * 2 );
		}

		public int WorkerCount { get; private set; }

		public ExecutionPerformanceMonitorOptions PerfMonOptions { get; private set; }

		public TaskProcessingOptions TaskProcessingOptions { get; private set; }

		public TaskQueueOptions TaskQueueOptions { get; private set; }
	}
}
