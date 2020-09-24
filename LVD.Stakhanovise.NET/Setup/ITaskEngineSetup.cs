using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface ITaskEngineSetup
	{
		ITaskEngine WithWorkerCount ( int workerCount );

		ITaskEngine WithPerformanceMonitorOptions ( ExecutionPerformanceMonitorOptions performanceMonitorOptions );

		ITaskEngine WithTaskProcessingOptions ( TaskProcessingOptions taskProcessingOptions );

		ITaskEngine WithTaskQueueOptions ( TaskQueueOptions taskQueueOptions );
	}
}
