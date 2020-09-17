using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class TimedExecutionResult<TResult>
	{
		public TimedExecutionResult ( TResult result, TimeSpan duration )
		{
			Result = result;
			Duration = duration;
			DurationMilliseconds = ( long )Math.Ceiling( duration.TotalMilliseconds );
		}

		public TResult Result { get; private set; }

		public TimeSpan Duration { get; private set; }

		public long DurationMilliseconds { get; private set; }

		public bool HasResult => !EqualityComparer<TResult>
			.Default
			.Equals( Result, default );
	}
}
