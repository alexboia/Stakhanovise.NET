using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueConsumerMetricsProvider : IAppMetricsProvider
	{
		void IncrementDequeueCount( TimeSpan duration );
	}
}
