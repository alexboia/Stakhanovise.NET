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
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardExecutionPerformanceMonitorSetup : IExecutionPerformanceMonitorSetup
	{
		private bool mFlushStats;

		private int mWriteCountThreshold;

		private int mWriteIntervalThresholdMilliseconds;

		public StandardExecutionPerformanceMonitorSetup ( StakhanoviseSetupDefaults defaults )
		{
			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mFlushStats = defaults
				.ExecutionPerformanceMonitorFlushStats;
			mWriteCountThreshold = defaults
				.ExecutionPerformanceMonitorWriteCountThreshold;
			mWriteIntervalThresholdMilliseconds = defaults
				.ExecutionPerformanceMonitorWriteIntervalThresholdMilliseconds;
		}

		public IExecutionPerformanceMonitorSetup FlushStats ( bool enabled )
		{
			mFlushStats = enabled;
			return this;
		}

		public IExecutionPerformanceMonitorSetup WithWriteCountThreshold ( int writeCountThreshold )
		{
			if ( writeCountThreshold < 1 )
				throw new ArgumentOutOfRangeException( nameof( writeCountThreshold ),
					"The write count threshold must be greater than or equal to 1" );

			mWriteCountThreshold = writeCountThreshold;
			return this;
		}

		public IExecutionPerformanceMonitorSetup WithWriteIntervalThresholdMilliseconds ( int writeIntervalThresholdMilliseconds )
		{
			if ( writeIntervalThresholdMilliseconds < 0 )
				throw new ArgumentOutOfRangeException( nameof( writeIntervalThresholdMilliseconds ), 
					"The write interval threshold must be greater than or equal to 0" );

			mWriteIntervalThresholdMilliseconds = writeIntervalThresholdMilliseconds;
			return this;
		}

		public ExecutionPerformanceMonitorOptions BuildOptions ()
		{
			return new ExecutionPerformanceMonitorOptions( mFlushStats,
				new ExecutionPerformanceMonitorWriteOptions( mWriteIntervalThresholdMilliseconds,
					mWriteCountThreshold ) );
		}
	}
}
