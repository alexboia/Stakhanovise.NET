using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskPollerSynchronizationPolicy : IAppMetricsProvider
	{
		void WaitForClearToDequeue( CancellationToken cancellationToken );

		void WaitForClearToAddToBuffer( CancellationToken cancellationToken );

		void NotifyPollerStarted();

		void NotifyPollerStopRequested();

		void Reset();
	}
}
