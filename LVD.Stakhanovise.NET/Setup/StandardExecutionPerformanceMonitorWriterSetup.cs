using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardExecutionPerformanceMonitorWriterSetup : IExecutionPerformanceMonitorWriterSetup
	{
		public IExecutionPerformanceMonitorWriterSetup SetupBuiltInWriter ( Action<IPostgreSqlExecutionPerformanceMonitorWriterSetup> setupAction )
		{
			throw new NotImplementedException();
		}

		public IExecutionPerformanceMonitorWriterSetup UseWriter ( IExecutionPerformanceMonitorWriter writer )
		{
			throw new NotImplementedException();
		}

		public IExecutionPerformanceMonitorWriterSetup UseWriterFactory ( Func<IExecutionPerformanceMonitorWriter> writerFactory )
		{
			throw new NotImplementedException();
		}
	}
}
