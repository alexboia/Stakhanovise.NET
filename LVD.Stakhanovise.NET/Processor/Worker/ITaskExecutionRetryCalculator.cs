using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutionRetryCalculator
	{
		DateTimeOffset ComputeRetryAt( IQueuedTaskToken queuedTaskToken );
	}
}
