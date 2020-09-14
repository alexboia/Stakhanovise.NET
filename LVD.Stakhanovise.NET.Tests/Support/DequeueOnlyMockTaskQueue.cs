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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class DequeueOnlyMockTaskQueue : ITaskQueueConsumer,
		ITaskQueueProducer,
		ITaskQueueStats,
		IDisposable
	{
		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsReceivingNewTaskUpdates;

		private Queue<QueuedTask> mProducedTasksBuffer =
			new Queue<QueuedTask>();

		private List<QueuedTask> mDequeuedTasksHistory =
			new List<QueuedTask>();

		private Task<bool> mQueueDepletedHandle;

		private TaskCompletionSource<bool> mQueueDepletedTaskCompletionSource;

		private Task mGenerationCompletedTask = Task.CompletedTask;

		private int mNumberOfTasks;

		private int mRemainingTaskCount;

		public DequeueOnlyMockTaskQueue ( int numberOfTasks )
		{
			mQueueDepletedTaskCompletionSource = new TaskCompletionSource<bool>();
			mQueueDepletedHandle = mQueueDepletedTaskCompletionSource.Task;

			mNumberOfTasks = numberOfTasks;
			mRemainingTaskCount = numberOfTasks;
		}

		private void NotifyClearToDequeue ()
		{
			EventHandler<ClearForDequeueEventArgs> handler = ClearForDequeue;
			if ( handler != null )
				handler( this, new ClearForDequeueEventArgs( ClearForDequeReason.NewTaskPostedNotificationReceived ) );
		}

		private Task GenerateNewTasksAsync ()
		{
			return Task.Delay( 150 ).ContinueWith( ante =>
			{
				QueuedTask queuedTask;
				int count = Math.Min( mRemainingTaskCount, 2 );

				for ( int iTask = 0; iTask < count; iTask++ )
				{
					queuedTask = new QueuedTask( Guid.NewGuid() );

					mDequeuedTasksHistory.Add( queuedTask );
					mProducedTasksBuffer.Enqueue( queuedTask );

					NotifyClearToDequeue();
				}

				mRemainingTaskCount = Math.Max( mRemainingTaskCount - count, 0 );
			} );
		}

		public async Task<QueuedTask> DequeueAsync ( params string[] supportedTypes )
		{
			await mGenerationCompletedTask;

			if ( mProducedTasksBuffer.Count != 0 )
				return mProducedTasksBuffer.Dequeue();

			if ( mRemainingTaskCount <= 0 )
				mQueueDepletedTaskCompletionSource.TrySetResult( false );
			else
				mGenerationCompletedTask = GenerateNewTasksAsync();

			return null;
		}

		public Task StartReceivingNewTaskUpdatesAsync ()
		{
			mIsReceivingNewTaskUpdates = true;
			return Task.Delay( 150 );
		}

		public Task StopReceivingNewTaskUpdatesAsync ()
		{
			mIsReceivingNewTaskUpdates = false;
			return Task.Delay( 150 );
		}

		public Task<QueuedTask> NotifyTaskCompletedAsync ( Guid queuedTaskId, TaskExecutionResult result )
		{
			throw new NotImplementedException();
		}

		public Task<QueuedTask> NotifyTaskErroredAsync ( Guid queuedTaskId, TaskExecutionResult result )
		{
			throw new NotImplementedException();
		}

		public Task NotifyTaskAuthorizationFailedAsync ( Guid queuedTaskId )
		{
			throw new NotImplementedException();
		}

		public Task<QueuedTask> PeekAsync ()
		{
			throw new NotImplementedException();
		}

		public Task ReleaseLockAsync ( Guid queuedTaskId )
		{
			throw new NotImplementedException();
		}

		public Task<QueuedTask> EnqueueAsync<TPayload> ( TPayload payload,
			string source,
			int priority )
		{
			throw new NotImplementedException();
		}

		public Task<TaskQueueMetrics> ComputeMetricsAsync ()
		{
			throw new NotImplementedException();
		}

		public void Dispose ()
		{
			mProducedTasksBuffer.Clear();
			mDequeuedTasksHistory.Clear();
		}

		public Task<bool> QueueDepletedHandle => mQueueDepletedHandle;

		public List<QueuedTask> DequeuedTasksHistory => mDequeuedTasksHistory;

		public bool IsReceivingNewTaskUpdates => mIsReceivingNewTaskUpdates;

		public int FaultErrorThresholdCount { get; set; }

		public int DequeuePoolSize => int.MaxValue;
	}
}
