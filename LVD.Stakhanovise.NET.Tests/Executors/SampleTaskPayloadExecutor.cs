using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class SampleTaskPayloadExecutor : ITaskExecutor<SampleTaskPayload>
	{
		public async Task ExecuteAsync ( SampleTaskPayload payload, ITaskExecutionContext executionContext )
		{
			await Task.Delay( 100 );
		}

		public async Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as SampleTaskPayload, executionContext );
		}

		public Type PayloadType => typeof( SampleTaskPayload );
	}
}
