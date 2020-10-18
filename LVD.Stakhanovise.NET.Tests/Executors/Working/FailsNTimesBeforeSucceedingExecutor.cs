using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Payloads.Working;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Executors.Working
{
	public class FailsNTimesBeforeSucceedingExecutor : ITaskExecutor<FailsNTimesBeforeSucceeding>
	{
		private static readonly ConcurrentDictionary<Guid, int> mFailCount =
			new ConcurrentDictionary<Guid, int>();

		public static void ResetFailCounts()
		{
			mFailCount.Clear();
		}

		public Task ExecuteAsync ( FailsNTimesBeforeSucceeding payload, ITaskExecutionContext executionContext )
		{
			int currentFailCount;

			if ( !mFailCount.TryGetValue( payload.Id, out currentFailCount ) )
				currentFailCount = 0;

			currentFailCount++;
			mFailCount.AddOrUpdate( payload.Id,
				addValueFactory: ( id ) => currentFailCount,
				updateValueFactory: ( id, old ) => currentFailCount );

			TestExecutorEventBus<FailsNTimesBeforeSucceeding>.Instance
				.NotifyExecutorCompleted();

			if ( currentFailCount <= payload.FailuresBeforeSuccess )
				throw new InvalidOperationException( $"Sample invalid operation during {nameof( FailsNTimesBeforeSucceeding )}" );
			else
				executionContext.NotifyTaskCompleted();

			return Task.CompletedTask;
		}

		public Task ExecuteAsync ( object payload, ITaskExecutionContext executionContext )
		{
			return ExecuteAsync( ( payload as FailsNTimesBeforeSucceeding ), executionContext );
		}

		public Type PayloadType => typeof( FailsNTimesBeforeSucceeding );
	}
}
