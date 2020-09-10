using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Executors
{
	public interface ITaskExecutor
	{
		Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext );

		Type PayloadType { get; }
	}
}
