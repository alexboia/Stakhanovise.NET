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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;

namespace LVD.Stakhanovise.NET
{
	public class TaskExecutionResult : IEquatable<TaskExecutionResult>
	{
		private TaskExecutionResultInfo mResultInfo;

		private long mRetryAtTicks;

		private long mProcessingTimeMilliseconds;

		private int mFaultErrorThresholdCount;

		[Obsolete]
		public TaskExecutionResult ( TimedExecutionResult<TaskExecutionResultInfo> resultInfo )
		{
			if ( resultInfo == null )
				throw new ArgumentNullException( nameof( resultInfo ) );

			mResultInfo = resultInfo.Result;
			mProcessingTimeMilliseconds = resultInfo.DurationMilliseconds;
		}

		[Obsolete]
		public TaskExecutionResult ( TimedExecutionResult<TaskExecutionResultInfo> resultInfo,
			long retryAtTicks )
			: this( resultInfo )
		{
			mRetryAtTicks = retryAtTicks;
		}

		public TaskExecutionResult ( TimedExecutionResult<TaskExecutionResultInfo> resultInfo,
			long retryAtTicks,
			int faultErrorThresholdCount )
			: this( resultInfo, retryAtTicks )
		{
			mFaultErrorThresholdCount = faultErrorThresholdCount;
		}

		public bool Equals ( TaskExecutionResult other )
		{
			return other != null &&
				ExecutedSuccessfully == other.ExecutedSuccessfully &&
				ExecutionCancelled == other.ExecutionCancelled &&
				IsRecoverable == other.IsRecoverable &&
				RetryAtTicks == other.RetryAtTicks &&
				FaultErrorThresholdCount == other.FaultErrorThresholdCount &&
				object.Equals( Error, other.Error );
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as TaskExecutionResult );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 31 + mResultInfo.ExecutedSuccessfully.GetHashCode();
			result = result * 31 + mResultInfo.ExecutionCancelled.GetHashCode();
			result = result * 31 + mResultInfo.IsRecoverable.GetHashCode();

			result = result * 31 + mRetryAtTicks.GetHashCode();
			result = result * 31 + mFaultErrorThresholdCount.GetHashCode();

			if ( mResultInfo.Error != null )
				result = result * 31 + mResultInfo.Error.GetHashCode();

			return result;
		}

		public int FaultErrorThresholdCount
			=> mFaultErrorThresholdCount;

		public bool ExecutedSuccessfully
			=> mResultInfo.ExecutedSuccessfully;

		public bool ExecutionCancelled
			=> mResultInfo.ExecutionCancelled;

		public long RetryAtTicks
			=> mRetryAtTicks;

		public long ProcessingTimeMilliseconds
			=> mProcessingTimeMilliseconds;

		public QueuedTaskError Error
			=> mResultInfo.Error;

		public bool IsRecoverable
			=> mResultInfo.IsRecoverable;
	}
}
