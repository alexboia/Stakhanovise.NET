using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskBufferMetricsProvider : IAppMetricsProvider
	{
		void IncrementTimesFilled();

		void IncrementTimesEmptied();

		void UpdateBufferCountStats( int newCount );
	}
}
