using LVD.Stakhanovise.NET.Queue;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskResultProcessor
	{
		Task ProcessResultAsync( IQueuedTaskToken queuedTaskToken, TaskExecutionResult result );
	}
}
