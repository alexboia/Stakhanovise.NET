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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StadardTaskProcessingSetup : ITaskProcessingSetup
	{
		private int mAbstractTimeTickTimeoutMilliseconds;

		private long mDefaultEstimatedProcessingTimeMilliseconds;

		private Func<int, long> mCalculateDelayTicksTaskAfterFailure;

		private Func<IQueuedTask, TaskExecutionStats, long> mCalculateEstimatedProcessingTimeMilliseconds;

		private Func<IQueuedTask, Exception, bool> mIsTaskErrorRecoverable;

		private int mFaultErrorThresholdCount;

		public StadardTaskProcessingSetup ( StakhanoviseSetupDefaults defaults )
		{
			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mAbstractTimeTickTimeoutMilliseconds = defaults
				.AbstractTimeTickTimeoutMilliseconds;
			mDefaultEstimatedProcessingTimeMilliseconds = defaults
				.DefaultEstimatedProcessingTimeMilliseconds;

			mCalculateDelayTicksTaskAfterFailure = defaults
				.CalculateDelayTicksTaskAfterFailure;
			mCalculateEstimatedProcessingTimeMilliseconds = defaults
				.CalculateEstimatedProcessingTimeMilliseconds;
			mIsTaskErrorRecoverable = defaults
				.IsTaskErrorRecoverable;
			//TODO: add to stakhanovise defaults
			mFaultErrorThresholdCount = 5;
		}

		public ITaskProcessingSetup WithAbstractTimeTickTimeoutMilliseconds ( int abstractTimeTickTimeoutMilliseconds )
		{
			if ( abstractTimeTickTimeoutMilliseconds <= 0 )
				throw new ArgumentOutOfRangeException( nameof( abstractTimeTickTimeoutMilliseconds ),
					"The timeout for the abstract time tick operation must be greater than or equal to 0" );

			mAbstractTimeTickTimeoutMilliseconds = abstractTimeTickTimeoutMilliseconds;
			return this;
		}

		public ITaskProcessingSetup WithDefaultEstimatedProcessingTimeMilliseconds ( long defaultEstimatedProcessingTimeMilliseconds )
		{
			if ( defaultEstimatedProcessingTimeMilliseconds < 1 )
				throw new ArgumentOutOfRangeException( nameof( defaultEstimatedProcessingTimeMilliseconds ),
					"The default estimated processing time must be greater than 1" );

			mDefaultEstimatedProcessingTimeMilliseconds = defaultEstimatedProcessingTimeMilliseconds;
			return this;
		}

		public ITaskProcessingSetup WithDelayTicksTaskAfterFailureCalculator ( Func<int, long> calculateDelayTicksTaskAfterFailure )
		{
			if ( calculateDelayTicksTaskAfterFailure == null )
				throw new ArgumentNullException( nameof( calculateDelayTicksTaskAfterFailure ) );

			mCalculateDelayTicksTaskAfterFailure = calculateDelayTicksTaskAfterFailure;
			return this;
		}

		public ITaskProcessingSetup WithEstimatedProcessingTimeMillisecondsCalculator ( Func<IQueuedTask, TaskExecutionStats, long> calculateEstimatedProcessingTimeMilliseconds )
		{
			if ( calculateEstimatedProcessingTimeMilliseconds == null )
				throw new ArgumentNullException( nameof( calculateEstimatedProcessingTimeMilliseconds ) );

			mCalculateEstimatedProcessingTimeMilliseconds = calculateEstimatedProcessingTimeMilliseconds;
			return this;
		}

		public ITaskProcessingSetup WithTaskErrorRecoverabilityCallback ( Func<IQueuedTask, Exception, bool> isTaskErrorRecoverable )
		{
			if ( isTaskErrorRecoverable == null )
				throw new ArgumentNullException( nameof( isTaskErrorRecoverable ) );

			mIsTaskErrorRecoverable = isTaskErrorRecoverable;
			return this;
		}

		public ITaskProcessingSetup WithFaultErrorThresholCount ( int faultErrorThresholdCount )
		{
			if ( faultErrorThresholdCount < 1 )
				throw new ArgumentOutOfRangeException( nameof( faultErrorThresholdCount ),
					"Fault error threshold count must be greater than or equal to 1" );

			mFaultErrorThresholdCount = faultErrorThresholdCount;
			return this;
		}

		public TaskProcessingOptions BuildOptions ()
		{
			return new TaskProcessingOptions( mAbstractTimeTickTimeoutMilliseconds,
				defaultEstimatedProcessingTimeMilliseconds: mDefaultEstimatedProcessingTimeMilliseconds,
				calculateDelayTicksTaskAfterFailure: mCalculateDelayTicksTaskAfterFailure,
				calculateEstimatedProcessingTimeMilliseconds: mCalculateEstimatedProcessingTimeMilliseconds,
				isTaskErrorRecoverable: mIsTaskErrorRecoverable,
				faultErrorThresholdCount: mFaultErrorThresholdCount );
		}
	}
}
