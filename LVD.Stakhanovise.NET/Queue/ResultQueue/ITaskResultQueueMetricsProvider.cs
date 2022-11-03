using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskResultQueueMetricsProvider : IAppMetricsProvider
	{
		void IncrementPostResultCount();

		void IncrementResultWriteCount( TimeSpan duration );

		void IncrementResultWriteRequestTimeoutCount();
	}
}
