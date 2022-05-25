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

namespace LVD.Stakhanovise.NET.Model
{
	public class TaskExecutionResultInfo : IEquatable<TaskExecutionResultInfo>
	{
		private QueuedTaskError mError;

		private bool mIsRecoverable;

		private TaskExecutionStatus mStatus;

		private TaskExecutionResultInfo ( TaskExecutionStatus status, QueuedTaskError error, bool isRecoverable )
		{
			mStatus = status;
			mError = error;
			mIsRecoverable = isRecoverable;
		}

		public static TaskExecutionResultInfo Successful ()
		{
			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutedSuccessfully,
				error: null,
				isRecoverable: false );
		}

		public static TaskExecutionResultInfo Cancelled ()
		{
			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutionCancelled,
				error: null,
				isRecoverable: false );
		}

		public static TaskExecutionResultInfo ExecutedWithError ( QueuedTaskError error, bool isRecoverable )
		{
			if ( error == null )
				throw new ArgumentNullException( nameof( error ) );

			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutedWithError,
				error: error,
				isRecoverable: isRecoverable );
		}

		public bool Equals ( TaskExecutionResultInfo other )
		{
			return other != null
				&& mStatus == other.mStatus
				&& object.Equals( mError, other.mError )
				&& mIsRecoverable == other.mIsRecoverable;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as TaskExecutionResultInfo );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + mStatus.GetHashCode();
			if ( mError != null )
				result = result * 13 + mError.GetHashCode();
			result = result * 13 + mIsRecoverable.GetHashCode();

			return result;
		}

		public bool ExecutedSuccessfully
			=> mStatus == TaskExecutionStatus.ExecutedSuccessfully;

		public bool ExecutionCancelled
			=> mStatus == TaskExecutionStatus.ExecutionCancelled;

		public bool ExecutionFailed
			=> mStatus == TaskExecutionStatus.ExecutedWithError;

		public QueuedTaskError Error
			=> mError;

		public bool IsRecoverable
			=> mIsRecoverable;
	}
}
