using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class SampleTaskPayloadWithResultExecutor : ITaskExecutor<SampleTaskPayloadWithResult>
	{
		public async Task ExecuteAsync( SampleTaskPayloadWithResult payload, ITaskExecutionContext executionContext )
		{
			await Task.Delay( 100 ).ContinueWith( t => executionContext.SetTaskCompleted() );
		}

		public async Task ExecuteAsync( object payload, ITaskExecutionContext executionContext )
		{
			await ExecuteAsync( payload as SampleTaskPayloadWithResult, executionContext );
		}

		public Type PayloadType => typeof( SampleTaskPayloadWithResult );
	}
}
