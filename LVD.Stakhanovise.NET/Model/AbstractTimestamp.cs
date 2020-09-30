// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class AbstractTimestamp
	{
		private long mTicks;

		private long mTickDuration;

		private long mWallclockTimeCost;

		private AbstractTimestamp ( long ticks, long wallclockTimeCost, long tickDuration )
		{
			mTicks = ticks;
			mWallclockTimeCost = wallclockTimeCost;
			mTickDuration = tickDuration;
		}

		public AbstractTimestamp ( long ticks, long wallclockTimeCost )
		{
			mTicks = ticks;
			mWallclockTimeCost = wallclockTimeCost;

			mTickDuration = ticks > 0
				? ( long )Math.Ceiling( ( double )wallclockTimeCost / ticks )
				: 0;
		}

		public static AbstractTimestamp Zero ()
		{
			return new AbstractTimestamp( 0, 0 );
		}

		public long GetTicksDurationForWallclockDuration ( TimeSpan duration )
		{
			return GetTicksDurationForWallclockDuration( ( long )Math.Ceiling( duration.TotalMilliseconds ) );
		}

		public long GetTicksDurationForWallclockDuration ( long wallclockMilliseconds )
		{
			return mTickDuration > 0
				? ( long )Math.Ceiling( ( double )wallclockMilliseconds * mTicks / mWallclockTimeCost )
				: 0;
		}

		public AbstractTimestamp AddWallclockTimeDuration ( TimeSpan duration )
		{
			return AddWallclockTimeDuration( ( long )Math.Ceiling( duration.TotalMilliseconds ) );
		}

		public AbstractTimestamp AddWallclockTimeDuration ( long duration )
		{
			long ticksForWallclockMilliseconds = 
				GetTicksDurationForWallclockDuration( duration );

			return new AbstractTimestamp( mTicks + ticksForWallclockMilliseconds,
				mWallclockTimeCost + duration );
		}

		public AbstractTimestamp FromTicks ( long ticks )
		{
			return new AbstractTimestamp( ticks, 
				mTickDuration * ticks, 
				mTickDuration );
		}

		public AbstractTimestamp AddTicks(long ticksToAdd)
		{
			return FromTicks( mTicks + ticksToAdd );
		}

		public AbstractTimestamp Copy ()
		{
			return new AbstractTimestamp( mTicks,
				mWallclockTimeCost );
		}

		public long Ticks
			=> mTicks;

		public long TickDuration
			=> mTickDuration;

		public long WallclockTimeCost
			=> mWallclockTimeCost;
	}
}
