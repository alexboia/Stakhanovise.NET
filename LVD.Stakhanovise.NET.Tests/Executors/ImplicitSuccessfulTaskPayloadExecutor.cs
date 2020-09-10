using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class ImplicitSuccessfulTaskPayloadExecutor : ITaskExecutor<ImplicitSuccessfulTaskPayload>
	{
		public Task ExecuteAsync ( ImplicitSuccessfulTaskPayload payload, ITaskExecutionContext executionContext )
		{
			return Task.CompletedTask;
		}

		public async Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as ImplicitSuccessfulTaskPayload, executionContext );
		}

		public Type PayloadType => typeof( ImplicitSuccessfulTaskPayload );
	}
}
