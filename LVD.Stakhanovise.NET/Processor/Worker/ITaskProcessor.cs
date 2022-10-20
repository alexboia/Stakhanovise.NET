using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskProcessor
	{
		Task<TaskProcessingResult> ProcessTaskAsync( TaskExecutionContext executionContext );
	}
}
