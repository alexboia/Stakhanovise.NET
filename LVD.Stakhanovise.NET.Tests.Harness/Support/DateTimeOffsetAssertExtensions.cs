using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class DateTimeOffsetAssertExtensions
	{
		public static void AssertEquals( this DateTimeOffset expected, DateTimeOffset actual, int millisecondDelta )
		{
			double actualMillisecondDelta = Math.Abs( ( expected - actual )
				.TotalMilliseconds );
			Assert.LessOrEqual( actualMillisecondDelta,
				( double ) millisecondDelta );
		}

		public static void AssertEquals( this DateTimeOffset? expected, DateTimeOffset? actual, int millisecondDelta )
		{
			if ( expected.HasValue )
			{
				Assert.IsTrue( actual.HasValue );
				expected.Value.AssertEquals( actual.Value, millisecondDelta );
			}
			else
				Assert.IsFalse( actual.HasValue );
		}
	}
}
