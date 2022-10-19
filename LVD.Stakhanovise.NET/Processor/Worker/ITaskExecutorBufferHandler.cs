using LVD.Stakhanovise.NET.Queue;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutorBufferHandler
	{
		void WaitForTaskAvailability();

		IQueuedTaskToken TryGetNextTask();
	}
}