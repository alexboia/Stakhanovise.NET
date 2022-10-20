using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutionResultProcessor
	{
		Task<TaskProcessingResult> ProcessResultAsync( TaskProcessingResult processingResult );
	}
}
