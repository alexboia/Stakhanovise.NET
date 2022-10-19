using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutorResolver
	{
		ITaskExecutor ResolveExecutor( IQueuedTask queuedTask );
	}
}
