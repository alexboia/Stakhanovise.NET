using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class AbstractTimestampTests
	{
		[Test]
		[TestCase( 1000, 2000, 2 )]
		[TestCase( 2000, 1000, 1 )]
		[TestCase( 40, 100, 3 )]
		public void Test_CanCreate ( long currentTicks,
			long currentTicksWallclockTimeCost,
			long excpectedTickDuration )
		{
			AbstractTimestamp ts = new AbstractTimestamp( currentTicks, currentTicksWallclockTimeCost );

			Assert.AreEqual( currentTicks, ts.Ticks );
			Assert.AreEqual( currentTicksWallclockTimeCost, ts.WallclockTimeCost );
			Assert.AreEqual( excpectedTickDuration, ts.TickDuration );
		}

		[Test]
		[TestCase( 1000, 2000 )]
		[TestCase( 2000, 1000 )]
		[TestCase( 40, 100 )]
		public void Test_CanGetAbstractTimeDurationForWallclockDuration ( long currentTicks,
			long currentTicksWallclockTimeCost )
		{
			AbstractTimestamp ts = new AbstractTimestamp( currentTicks, currentTicksWallclockTimeCost );

			long abstractTimeDuration = ts.GetTicksDurationForWallclockDuration( 1000 );

			Assert.AreEqual( ( long )Math.Ceiling( ( double )1000 * currentTicks / currentTicksWallclockTimeCost ),
				abstractTimeDuration );
		}
	}
}
