using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Executors
{
	public abstract class BaseTaskExecutor<TPayload> : ITaskExecutor<TPayload>
	{
		public virtual async Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( ( TPayload )payload, executionContext );
		}

		public abstract Task ExecuteAsync ( TPayload payload, ITaskExecutionContext executionContext );

		public virtual Type PayloadType => typeof( TPayload );
	}
}
