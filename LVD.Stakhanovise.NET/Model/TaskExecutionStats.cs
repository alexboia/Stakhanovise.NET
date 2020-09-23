using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class TaskExecutionStats
	{
		public TaskExecutionStats ( long lastExecutionTime,
			long averageExecutionTime,
			long fastestExecutionTime,
			long longestExecutionTime,
			long totalExecutionTime,
			long numberOfExecutionCycles )
		{
			LastExecutionTime = lastExecutionTime;
			AverageExecutionTime = averageExecutionTime;
			FastestExecutionTime = fastestExecutionTime;
			LongestExecutionTime = longestExecutionTime;
			TotalExecutionTime = totalExecutionTime;
			NumberOfExecutionCycles = numberOfExecutionCycles;
		}

		public static TaskExecutionStats Initial(long executionTime)
		{
			return new TaskExecutionStats( lastExecutionTime: executionTime, 
				averageExecutionTime: executionTime, 
				longestExecutionTime: executionTime, 
				fastestExecutionTime: executionTime, 
				totalExecutionTime: executionTime, 
				numberOfExecutionCycles: 1 );
		}

		public TaskExecutionStats UpdateWithNewCycleExecutionTime ( long executionTime )
		{
			long fastestExecutionTime = Math.Min( FastestExecutionTime, executionTime );
			long longestExecutionTime = Math.Max( LongestExecutionTime, executionTime );
			long totalExecutionTime = TotalExecutionTime + executionTime;
			long numberOfExecutionCycles = NumberOfExecutionCycles + 1;
			long averageExecutionTime = ( long )Math.Ceiling( ( double )totalExecutionTime / numberOfExecutionCycles );

			return new TaskExecutionStats( executionTime,
				averageExecutionTime,
				fastestExecutionTime,
				longestExecutionTime,
				totalExecutionTime,
				numberOfExecutionCycles );
		}

		public static TaskExecutionStats Zero ()
		{
			return new TaskExecutionStats( 0, 0, 0, 0, 0, 0 );
		}

		public long NumberOfExecutionCycles { get; private set; }

		public long LastExecutionTime { get; private set; }

		public long AverageExecutionTime { get; private set; }

		public long FastestExecutionTime { get; private set; }

		public long LongestExecutionTime { get; private set; }

		public long TotalExecutionTime { get; private set; }
	}
}
