using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class DateTimeOffsetAssertExtensions
	{
		public static void AssertEquals( this DateTimeOffset expected, DateTimeOffset actual, int millisecondDelta )
		{
			double actualMillisecondDelta = Math.Abs( ( expected - actual )
				.TotalMilliseconds );
			ClassicAssert.LessOrEqual( actualMillisecondDelta,
				( double ) millisecondDelta );
		}

		public static void AssertEquals( this DateTimeOffset? expected, DateTimeOffset? actual, int millisecondDelta )
		{
			if ( expected.HasValue )
			{
				ClassicAssert.IsTrue( actual.HasValue );
				expected.Value.AssertEquals( actual.Value, millisecondDelta );
			}
			else
				ClassicAssert.IsFalse( actual.HasValue );
		}
	}
}
