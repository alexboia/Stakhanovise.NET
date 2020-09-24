using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IPerformanceMonitorWriterSetup
	{
		IPerformanceMonitorWriterSetup UseWriter ( IExecutionPerformanceMonitorWriter writer );

		IPerformanceMonitorWriterSetup UseTimingBelt ( Func<IExecutionPerformanceMonitorWriter> writerFactory );

		IPerformanceMonitorWriterSetup SetupBuiltInWriter ( Action<IPostgreSqlExecutionPerformanceMonitorWriterSetup> setupAction );
	}
}
