using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class ReasonableStakhanoviseDefaultsProvider : IStakhanoviseSetupDefaultsProvider
	{
		public StakhanoviseSetupDefaults GetDefaults ()
		{
			int defaultWorkerCount = Math.Max( 1, Environment.ProcessorCount - 1 );

			StakhanoviseSetupDefaults defaults = new StakhanoviseSetupDefaults()
			{
				Mapping = new QueuedTaskMapping(),
				ExecutorAssemblies = GetDefaultAssembliesToScan(),
				WorkerCount = defaultWorkerCount,

				CalculateDelayTicksTaskAfterFailure = token
					=> ( long )Math.Pow( 10, token.LastQueuedTaskResult.ErrorCount + 1 ),

				IsTaskErrorRecoverable = ( task, exc )
					=> !( exc is NullReferenceException )
						&& !( exc is ArgumentException ),

				FaultErrorThresholdCount = 5,

				AppMetricsCollectionIntervalMilliseconds = 10000,
				AppMetricsMonitoringEnabled = true,
				SetupBuiltInDbAsssets = true
			};

			return defaults;
		}

		private Assembly[] GetDefaultAssembliesToScan ()
		{
			return new Assembly[]
			{
				Assembly.GetExecutingAssembly()
			};
		}
	}
}
