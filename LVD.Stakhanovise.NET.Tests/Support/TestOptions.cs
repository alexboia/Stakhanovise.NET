using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class TestOptions
	{
		public static TaskProcessingOptions GetTaskProcessingOptions ()
		{
			return new TaskProcessingOptions( 1000,
				defaultEstimatedProcessingTimeMilliseconds: 1000,
				calculateDelayTicksTaskAfterFailure: errorCount
					=> ( long )Math.Pow( 10, errorCount ),
				calculateEstimatedProcessingTimeMilliseconds: ( task, stats )
					=> stats.LongestExecutionTime > 0
						? stats.LongestExecutionTime
						: 1000,
				isTaskErrorRecoverable: ( task, exc )
					 => !( exc is NullReferenceException )
						 && !( exc is ArgumentException ) );
		}
	}
}
