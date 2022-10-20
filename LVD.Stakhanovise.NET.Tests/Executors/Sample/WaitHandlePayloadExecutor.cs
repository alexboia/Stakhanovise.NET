using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class WaitHandlePayloadExecutor : ITaskExecutor<WaitHandlePayload>
	{
		public Task ExecuteAsync( WaitHandlePayload payload, ITaskExecutionContext executionContext )
		{
			payload.WaitHandle.WaitOne();
			return Task.CompletedTask;
		}

		public async Task ExecuteAsync( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as WaitHandlePayload, executionContext );
		}

		public Type PayloadType => typeof( WaitHandlePayload );
	}
}
