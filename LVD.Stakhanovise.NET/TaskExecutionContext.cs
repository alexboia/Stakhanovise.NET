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
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LVD.Stakhanovise.NET
{
	public class TaskExecutionContext : ITaskExecutionContext
	{
		private IQueuedTask mTask;

		private TaskExecutionResultInfo mResult;

		private CancellationToken mCancellationToken;

		public TaskExecutionContext ( IQueuedTaskToken taskToken )
			: this( taskToken.QueuedTask, taskToken.CancellationToken )
		{
			return;
		}

		public TaskExecutionContext ( IQueuedTask task, CancellationToken cancellationToken )
		{
			mTask = task ?? throw new ArgumentNullException( nameof( task ) );
			mCancellationToken = cancellationToken;
		}

		public void NotifyTaskCompleted ()
		{
			mResult = TaskExecutionResultInfo.Successful();
		}

		public void NotifyTaskErrored ( QueuedTaskError error, bool isRecoverable )
		{
			mResult = TaskExecutionResultInfo.ExecutedWithError( error, isRecoverable );
		}

		public void NotifyCancellationObserved ()
		{
			mResult = TaskExecutionResultInfo.Cancelled();
		}

		public void ThrowIfCancellationRequested ()
		{
			mCancellationToken.ThrowIfCancellationRequested();
		}

		public IQueuedTask Task => mTask;

		public TaskExecutionResultInfo ResultInfo => mResult;

		public QueuedTaskStatus TaskStatus => mTask.Status;

		public bool IsCancellationRequested => mCancellationToken.IsCancellationRequested;

		public bool HasResult => mResult != null;
	}
}
