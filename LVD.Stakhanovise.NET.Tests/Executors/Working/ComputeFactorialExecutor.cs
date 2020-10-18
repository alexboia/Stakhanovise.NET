using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class ComputeFactorialExecutor : ITaskExecutor<ComputeFactorial>
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public Type PayloadType => typeof( ComputeFactorial );

		public Task ExecuteAsync ( ComputeFactorial payload, ITaskExecutionContext executionContext )
		{
			long result = 1;

			if ( payload.ForN > 0 )
			{
				for ( int i = 1; i <= payload.ForN; i++ )
					result = result * i;
			}

			mLogger.DebugFormat( "Factorial for {0} = {1}",
				payload.ForN,
				result );

			TestExecutorEventBus<ComputeFactorial>.Instance
				.NotifyExecutorCompleted();

			return Task.CompletedTask;
		}

		public Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			return ExecuteAsync( ( payload as ComputeFactorial ), executionContext );
		}
	}
}
