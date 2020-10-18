using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class ComparisonExtensions
	{
		public static bool EqualsAproximately ( this DateTimeOffset actual, DateTimeOffset expected, int withinMilliseconds = 250 )
		{
			return Math.Abs( ( expected - actual ).TotalMilliseconds ) <= withinMilliseconds;
		}

		public static bool EqualsAproximately ( this DateTimeOffset? actual, DateTimeOffset? expected, int withinMilliseconds = 250 )
		{
			return Math.Abs( ( expected - actual ).Value.TotalMilliseconds ) <= withinMilliseconds;
		}
	}
}
