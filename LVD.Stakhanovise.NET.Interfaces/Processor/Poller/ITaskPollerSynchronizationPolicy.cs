using System.Threading;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskPollerSynchronizationPolicy
	{
		void WaitForClearToDequeue( CancellationToken cancellationToken );

		void WaitForClearToAddToBuffer( CancellationToken cancellationToken );

		void NotifyPollerStarted();

		void NotifyPollerStopRequested();

		void Reset();
	}
}
