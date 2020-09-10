using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class ThrowsExceptionTaskPayloadExecutor : ITaskExecutor<ThrowsExceptionTaskPayload>
	{
		public Task ExecuteAsync ( ThrowsExceptionTaskPayload payload, ITaskExecutionContext executionContext )
		{
			throw new InvalidOperationException( "Sample invalid operation exception throw directly" );
		}

		public async Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as ThrowsExceptionTaskPayload, executionContext );
		}

		public Type PayloadType => typeof( ThrowsExceptionTaskPayload );
	}
}
