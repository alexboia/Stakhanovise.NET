using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class SuccessfulTaskPayloadExecutor : ITaskExecutor<SuccessfulTaskPayload>
	{
		public Task ExecuteAsync ( SuccessfulTaskPayload payload, ITaskExecutionContext executionContext )
		{
			executionContext.NotifyTaskCompleted();
			return Task.CompletedTask;
		}

		public Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			return ExecuteAsync( payload as SuccessfulTaskPayload, executionContext );
		}

		public Type PayloadType => typeof( SuccessfulTaskPayload );
	}
}
