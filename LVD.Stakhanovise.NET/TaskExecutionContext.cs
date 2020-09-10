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
using System;
using System.Collections.Concurrent;

namespace LVD.Stakhanovise.NET
{
	public class TaskExecutionContext : ITaskExecutionContext
	{
		private QueuedTask mTask;

		private ConcurrentDictionary<string, object> mContextData =
		   new ConcurrentDictionary<string, object>();

		private TaskExecutionResult mResult;

		public TaskExecutionContext ( QueuedTask task )
		{
			mTask = task
				?? throw new ArgumentNullException( nameof( task ) );
		}

		public void NotifyTaskCompleted ()
		{
			mResult = new TaskExecutionResult( mTask );
		}

		public void NotifyTaskErrored ( QueuedTaskError error, bool isRecoverable )
		{
			mResult = new TaskExecutionResult( mTask,
				error,
				isRecoverable );
		}

		public TValue Get<TValue> ( string key )
		{
			if ( string.IsNullOrEmpty( key ) )
				throw new ArgumentNullException( nameof( key ) );

			object value;
			if ( !mContextData.TryGetValue( key, out value ) )
				value = null;

			return value is TValue
				? ( TValue )value
				: default( TValue );
		}

		public void Set<TValue> ( string key, TValue value )
		{
			if ( string.IsNullOrEmpty( key ) )
				throw new ArgumentNullException( nameof( key ) );

			TValue current = Get<TValue>( key );
			mContextData.TryUpdate( key,
			   value,
			   current );
		}

		public QueuedTask Task => mTask;

		public TaskExecutionResult Result => mResult;

		public QueuedTaskStatus TaskStatus => mTask.Status;

		public bool HasResult => mResult != null;
	}
}
