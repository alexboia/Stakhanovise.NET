using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class CancellationObservedPayloadExecutor : ITaskExecutor<CancellationObservedPayload>
	{
		public Task ExecuteAsync( CancellationObservedPayload payload, ITaskExecutionContext executionContext )
		{
			payload.SyncHandle.WaitOne();
			executionContext.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		public async Task ExecuteAsync( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as CancellationObservedPayload, executionContext );
		}

		public Type PayloadType => typeof( CancellationObservedPayload );
	}
}
