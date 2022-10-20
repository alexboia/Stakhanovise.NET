using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutionMetricsProvider : IAppMetricsProvider
	{
		void UpdateTaskProcessingStats( TaskProcessingResult processingResult );

		void IncrementBufferWaitCount();
	}
}
