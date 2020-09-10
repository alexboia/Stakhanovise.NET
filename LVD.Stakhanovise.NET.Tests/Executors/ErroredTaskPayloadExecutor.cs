using LVD.Stakhanovise.NET;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class ErroredTaskPayloadExecutor : ITaskExecutor<ErroredTaskPayload>
	{
		public Task ExecuteAsync ( ErroredTaskPayload payload, ITaskExecutionContext executionContext )
		{
			executionContext.NotifyTaskErrored( new QueuedTaskError( new InvalidOperationException( "Sample invalid operation" ) ), isRecoverable: true );
			return Task.CompletedTask;
		}

		public async Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as ErroredTaskPayload, executionContext );
		}

		public Type PayloadType => typeof( ErroredTaskPayload );
	}
}
