using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class ComputeFactorialExecutor : ITaskExecutor<ComputeFactorial>
	{
		public Type PayloadType => typeof( ComputeFactorial );

		public Task ExecuteAsync ( ComputeFactorial payload, ITaskExecutionContext executionContext )
		{
			long result = 1;

			if ( payload.ForN > 0 )
			{
				for ( int i = 1; i <= payload.ForN; i++ )
					result = result * i;
			}

			ResultStorage<ComputeFactorialResult>.Instance.Add( new ComputeFactorialResult( payload.ForN, result ) );

			return Task.CompletedTask;
		}

		public Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			return ExecuteAsync( ( payload as ComputeFactorial ), executionContext );
		}
	}
}
