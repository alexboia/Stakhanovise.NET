﻿// 
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
	public class TaskExecutionStats : IEquatable<TaskExecutionStats>
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

		public static TaskExecutionStats Initial ( long executionTime )
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

		public TaskExecutionStats Since ( TaskExecutionStats previous )
		{
			long prevTotalExecutionTime = 0;
			long prevNumberOfExecutionCycles = 0;
			long prevFastestExecutionTime = long.MaxValue;
			long prevLongestExecutionTime = long.MinValue;

			if ( previous != null )
			{
				prevTotalExecutionTime = previous.TotalExecutionTime;
				prevNumberOfExecutionCycles = previous.NumberOfExecutionCycles;
				prevFastestExecutionTime = previous.FastestExecutionTime;
				prevLongestExecutionTime = previous.LongestExecutionTime;
			}

			if ( NumberOfExecutionCycles - prevNumberOfExecutionCycles > 0 )
			{
				return new TaskExecutionStats( lastExecutionTime: LastExecutionTime,
					averageExecutionTime: ( long )Math.Ceiling(
						( double )( TotalExecutionTime - prevTotalExecutionTime )
							/ ( NumberOfExecutionCycles - prevNumberOfExecutionCycles )
					),
					fastestExecutionTime: Math.Min( FastestExecutionTime,
						prevFastestExecutionTime ),
					longestExecutionTime: Math.Max( LongestExecutionTime,
						prevLongestExecutionTime ),
					totalExecutionTime: ( TotalExecutionTime
						- prevTotalExecutionTime ),
					numberOfExecutionCycles: ( NumberOfExecutionCycles
						- prevNumberOfExecutionCycles ) );
			}
			else
			{
				return new TaskExecutionStats( lastExecutionTime: LastExecutionTime,
					averageExecutionTime: 0,
					fastestExecutionTime: FastestExecutionTime,
					longestExecutionTime: LongestExecutionTime,
					totalExecutionTime: 0,
					numberOfExecutionCycles: 0 );
			}
		}

		public TaskExecutionStats Copy ()
		{
			return new TaskExecutionStats( LastExecutionTime,
				averageExecutionTime: AverageExecutionTime,
				fastestExecutionTime: FastestExecutionTime,
				longestExecutionTime: LongestExecutionTime,
				totalExecutionTime: TotalExecutionTime,
				numberOfExecutionCycles: NumberOfExecutionCycles );
		}

		public static TaskExecutionStats Zero ()
		{
			return new TaskExecutionStats( 0, 0, 0, 0, 0, 0 );
		}

		public bool Equals ( TaskExecutionStats other )
		{
			return other != null
				&& other.NumberOfExecutionCycles == NumberOfExecutionCycles
				&& other.LastExecutionTime == LastExecutionTime
				&& other.AverageExecutionTime == AverageExecutionTime
				&& other.FastestExecutionTime == FastestExecutionTime
				&& other.TotalExecutionTime == TotalExecutionTime
				&& other.LongestExecutionTime == LongestExecutionTime;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as TaskExecutionStats );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + NumberOfExecutionCycles.GetHashCode();
			result = result * 13 + LastExecutionTime.GetHashCode();
			result = result * 13 + AverageExecutionTime.GetHashCode();
			result = result * 13 + FastestExecutionTime.GetHashCode();
			result = result * 13 + LongestExecutionTime.GetHashCode();
			result = result * 13 + TotalExecutionTime.GetHashCode();

			return result;
		}

		public long NumberOfExecutionCycles { get; private set; }

		public long LastExecutionTime { get; private set; }

		public long AverageExecutionTime { get; private set; }

		public long FastestExecutionTime { get; private set; }

		public long LongestExecutionTime { get; private set; }

		public long TotalExecutionTime { get; private set; }
	}
}
