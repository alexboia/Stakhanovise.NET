// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;

namespace LVD.Stakhanovise.NET
{
	public class TaskExecutionResult : IEquatable<TaskExecutionResult>
	{
		private TaskExecutionResultInfo mResultInfo;

		private DateTimeOffset mRetryAt;

		private long mProcessingTimeMilliseconds;

		private int mFaultErrorThresholdCount;

		public TaskExecutionResult ( TaskExecutionResultInfo resultInfo,
			TimeSpan duration,
			DateTimeOffset retryAt,
			int faultErrorThresholdCount )
		{
			if ( resultInfo == null )
				throw new ArgumentNullException( nameof( resultInfo ) );

			mResultInfo = resultInfo;
			mFaultErrorThresholdCount = faultErrorThresholdCount;
			mProcessingTimeMilliseconds = ( long )Math.Ceiling( duration.TotalMilliseconds );
			mRetryAt = retryAt;
		}

		public bool Equals ( TaskExecutionResult other )
		{
			return other != null
				&& mResultInfo.Equals( other.mResultInfo )
				&& mRetryAt == other.mRetryAt
				&& mProcessingTimeMilliseconds == other.mProcessingTimeMilliseconds;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as TaskExecutionResult );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 31 + mResultInfo.GetHashCode();
			result = result * 31 + mFaultErrorThresholdCount.GetHashCode();
			result = result * 31 + mProcessingTimeMilliseconds.GetHashCode();
			result = result * 31 + mRetryAt.GetHashCode();

			return result;
		}

		public bool HasResult
			=> mResultInfo != null;

		public int FaultErrorThresholdCount
			=> mFaultErrorThresholdCount;

		public bool ExecutedSuccessfully
			=> mResultInfo.ExecutedSuccessfully;

		public bool ExecutionCancelled
			=> mResultInfo.ExecutionCancelled;

		public bool ExecutionFailed
			=> mResultInfo.ExecutionFailed;

		public DateTimeOffset RetryAt
			=> mRetryAt;

		public long ProcessingTimeMilliseconds
			=> mProcessingTimeMilliseconds;

		public QueuedTaskError Error
			=> mResultInfo.Error;

		public bool IsRecoverable
			=> mResultInfo.IsRecoverable;
	}
}
