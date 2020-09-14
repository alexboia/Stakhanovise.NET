using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class AbstractTimestamp
	{
		private long mCurrentTicks;

		private long mTickDuration;

		private long mCurrentTicksWallclockTimeCost;

		public AbstractTimestamp ( long currentTicks, long currentTicksWallclockTimeCost )
		{
			mCurrentTicks = currentTicks;
			mCurrentTicksWallclockTimeCost = currentTicksWallclockTimeCost;

			mTickDuration = currentTicksWallclockTimeCost > 0 
				? currentTicks / currentTicksWallclockTimeCost 
				: 0;
		}

		public static AbstractTimestamp Zero()
		{
			return new AbstractTimestamp( 0, 0 );
		}

		public long GetAbstractTimeDurationForWallclockDuration ( TimeSpan duration )
		{
			return GetAbstractTimeDurationForWallclockDuration( ( long )duration.TotalMilliseconds );
		}

		public long GetAbstractTimeDurationForWallclockDuration ( long wallclockMilliseconds )
		{
			return mTickDuration > 0 
				? wallclockMilliseconds / mTickDuration 
				: 0;
		}

		public AbstractTimestamp Copy ()
		{
			return new AbstractTimestamp( mCurrentTicks, 
				mCurrentTicksWallclockTimeCost );
		}

		public long CurrentTicks
			=> mCurrentTicks;

		public long TickDuration
			=> mTickDuration;

		public long CurrentTicksWallclockTimeCost
			=> mCurrentTicksWallclockTimeCost;
	}
}
