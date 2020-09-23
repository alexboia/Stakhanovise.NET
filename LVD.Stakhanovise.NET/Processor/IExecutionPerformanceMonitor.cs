using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface IExecutionPerformanceMonitor
	{
		void ReportExecutionTime ( Type payloadType, long durationMilliseconds );

		TaskExecutionStats GetExecutionTimeInfo ( Type payloadType );

		Task StartFlushingAsync ( IExecutionPerformanceMonitorWriter writer, ExecutionPerformanceMonitorWriteOptions options );

		Task StopFlushingAsync ();

		IReadOnlyDictionary<Type, TaskExecutionStats> ExecutionTimeInfo { get; }
	}
}
