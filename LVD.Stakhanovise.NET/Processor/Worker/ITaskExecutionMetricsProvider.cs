using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutionMetricsProvider : IAppMetricsProvider
	{
		void UpdateTaskProcessingStats( TaskExecutionResult result );

		void IncrementBufferWaitCount();
	}
}
