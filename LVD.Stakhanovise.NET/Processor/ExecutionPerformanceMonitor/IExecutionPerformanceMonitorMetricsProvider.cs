using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface IExecutionPerformanceMonitorMetricsProvider : IAppMetricsProvider
	{
		void IncrementPerfMonPostCount();

		void IncrementPerfMonWriteCount( TimeSpan duration );

		void IncrementPerfMonWriteRequestTimeoutCount();
	}
}
