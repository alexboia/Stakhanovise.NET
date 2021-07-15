// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
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
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class TaskProcessingOptions
	{
		public TaskProcessingOptions ( Func<IQueuedTaskToken, long> calculateDelayTicksTaskAfterFailure,
			Func<IQueuedTask, Exception, bool> isTaskErrorRecoverable,
			int faultErrorThresholdCount )
		{
			if ( calculateDelayTicksTaskAfterFailure == null )
				throw new ArgumentNullException( nameof( calculateDelayTicksTaskAfterFailure ) );

			if ( isTaskErrorRecoverable == null )
				throw new ArgumentNullException( nameof( isTaskErrorRecoverable ) );

			if ( faultErrorThresholdCount < 1 )
				throw new ArgumentOutOfRangeException( nameof( faultErrorThresholdCount ),
					"Fault error threshold count must be greater than or equal to 1" );

			CalculateRetryMillisecondsDelay = calculateDelayTicksTaskAfterFailure;
			IsTaskErrorRecoverable = isTaskErrorRecoverable;
			FaultErrorThresholdCount = faultErrorThresholdCount;
		}

		public Func<IQueuedTaskToken, long> CalculateRetryMillisecondsDelay { get; private set; }

		public Func<IQueuedTask, Exception, bool> IsTaskErrorRecoverable { get; private set; }

		public int FaultErrorThresholdCount { get; private set; }
	}
}
