using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Payloads.Working;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors.Working
{
	public class AlwaysFailingTaskExecutor : ITaskExecutor<AlwaysFailingTask>
	{
		public Task ExecuteAsync ( AlwaysFailingTask payload, ITaskExecutionContext executionContext )
		{
			TestExecutorEventBus<AlwaysFailingTask>.Instance
				.NotifyExecutorCompleted();

			throw new InvalidOperationException( $"Sample invalid operation during {nameof( AlwaysFailingTask )}" );
		}

		public Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			return ExecuteAsync( ( payload as AlwaysFailingTask ), executionContext );
		}

		public Type PayloadType => typeof( AlwaysFailingTask );
	}
}
