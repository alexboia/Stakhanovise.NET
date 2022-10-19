using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface ITaskExecutorBufferHandlerFactory
	{
		ITaskExecutorBufferHandler Create( ITaskBuffer buffer, CancellationToken cancellationToken );
	}
}
