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
using log4net;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultTaskResultQueue : ITaskResultQueue
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private ITaskQueueConsumer mTaskQueueConsumer;

		private bool mIsDisposed;

		public DefaultTaskResultQueue ( ITaskQueueConsumer taskQueueConsumer )
		{
			mTaskQueueConsumer = taskQueueConsumer 
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskResultQueue ), "Cannot reuse a disposed task result queue" );
		}

		public async Task EnqueueResultAsync ( QueuedTask queuedTask, TaskExecutionResult result )
		{
			CheckDisposedOrThrow();

			if ( queuedTask == null )
				throw new ArgumentNullException( nameof( queuedTask ) );

			try
			{
				if ( result != null )
				{
					//If the task did not execute successfully, notify the queue of the error;
					//  otherwise mark it as completed
					if ( !result.ExecutedSuccessfully )
						await mTaskQueueConsumer.NotifyTaskErroredAsync( queuedTask.Id,
							result );
					else
						await mTaskQueueConsumer.NotifyTaskCompletedAsync( queuedTask.Id,
							result );
				}

				//If there is no result, simply release the task - 
				//  we don't really have anything else to do
				else
					await mTaskQueueConsumer.ReleaseLockAsync( queuedTask.Id );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error finalizing task processing", exc );
			}
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				//We are not responsible for managing the lifecycle 
				//  of the task queue, so we will not be disposing it over here
				if ( disposing )
					mTaskQueueConsumer = null;

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
		}
	}
}
