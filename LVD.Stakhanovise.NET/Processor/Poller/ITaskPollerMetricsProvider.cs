using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskPollerMetricsProvider : IAppMetricsProvider
	{
		void IncrementPollerDequeueCount();

		void IncrementPollerReturnedTaskCount();

		void IncrementPollerWaitForBufferSpaceCount();

		void IncrementPollerWaitForDequeueCount();
	}
}
