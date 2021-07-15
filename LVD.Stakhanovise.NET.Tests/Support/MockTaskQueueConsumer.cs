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
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockTaskQueueConsumer : ITaskQueueConsumer, IDisposable
	{
		private const int TaskGenerationTimeoutMilliseconds = 150;

		private const int MaxSizeOfGeneratedTasksBatch = 2;

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsReceivingNewTaskUpdates;

		private Queue<IQueuedTaskToken> mProducedTasksBuffer =
			new Queue<IQueuedTaskToken>();

		private List<IQueuedTaskToken> mDequeuedTasksHistory =
			new List<IQueuedTaskToken>();

		private Task<bool> mQueueDepletedHandle;

		private TaskCompletionSource<bool> mQueueDepletedTaskCompletionSource;

		private Task mGenerationCompletedTask = Task.CompletedTask;

		private int mNumberOfTasksToGenerate;

		private int mRemainingTaskCount;

		private ITimestampProvider mTimeProvider;

		private int mDequeueCallCount = 0;

		public MockTaskQueueConsumer( int numberOfTasksToGenerate, ITimestampProvider timeProvider )
		{
			//TODO: also add task type
			mQueueDepletedTaskCompletionSource = new TaskCompletionSource<bool>();
			mQueueDepletedHandle = mQueueDepletedTaskCompletionSource.Task;

			mNumberOfTasksToGenerate = numberOfTasksToGenerate;
			mRemainingTaskCount = numberOfTasksToGenerate;
			mTimeProvider = timeProvider;
		}

		private void NotifyClearToDequeue()
		{
			EventHandler<ClearForDequeueEventArgs> handler = ClearForDequeue;
			if ( handler != null )
				handler( this, new ClearForDequeueEventArgs( ClearForDequeReason.NewTaskPostedNotificationReceived ) );
		}

		private Task GenerateNewTasksAsync()
		{
			return Task.Delay( TaskGenerationTimeoutMilliseconds ).ContinueWith( ante =>
			{
				int generateCount = ComputeGenerateTasksCount();
				GenerateTasks( generateCount );
				UpdateRemainingTasksCount( generateCount );
			} );
		}

		private int ComputeGenerateTasksCount()
		{
			return Math.Min( mRemainingTaskCount, MaxSizeOfGeneratedTasksBatch );
		}

		private void GenerateTasks( int generateCount )
		{
			for ( int iTask = 0; iTask < generateCount; iTask++ )
			{
				QueuedTask queuedTask = GenerateRandomTask();
				StoreGeneratedTask( queuedTask );
				NotifyClearToDequeue();
			}
		}

		private QueuedTask GenerateRandomTask()
		{
			return new QueuedTask( Guid.NewGuid() );
		}

		private void StoreGeneratedTask( QueuedTask queuedTask )
		{
			mDequeuedTasksHistory.Add( new MockQueuedTaskToken( queuedTask,
				new QueuedTaskResult( queuedTask ) ) );
			mProducedTasksBuffer.Enqueue( new MockQueuedTaskToken( queuedTask,
				new QueuedTaskResult( queuedTask ) ) );
		}

		private void UpdateRemainingTasksCount( int generateCount )
		{
			mRemainingTaskCount = Math.Max( mRemainingTaskCount - generateCount, 0 );
		}

		public async Task<IQueuedTaskToken> DequeueAsync( params string[] supportedTypes )
		{
			IncrementDequeueCallCount();
			await WaitForTaskGenerationCompletionAsync();

			if ( HasAlreadyProducedTasks() )
				return FetchAndRemoveProducedTask();

			if ( HasAnyMoreTasksToGenerate() )
				StartGeneratingNewTasks();
			else
				SetTaskQueueDepleted();

			return null;
		}

		private void IncrementDequeueCallCount()
		{
			mDequeueCallCount++;
		}

		private async Task WaitForTaskGenerationCompletionAsync()
		{
			await mGenerationCompletedTask;
		}

		private bool HasAlreadyProducedTasks()
		{
			return mProducedTasksBuffer.Count > 0;
		}

		private IQueuedTaskToken FetchAndRemoveProducedTask()
		{
			return mProducedTasksBuffer.Dequeue();
		}

		private bool HasAnyMoreTasksToGenerate()
		{
			return mRemainingTaskCount > 0;
		}

		private void StartGeneratingNewTasks()
		{
			mGenerationCompletedTask = GenerateNewTasksAsync();
		}

		private void SetTaskQueueDepleted()
		{
			mQueueDepletedTaskCompletionSource.TrySetResult( true );
		}

		public IQueuedTaskToken Dequeue( params string[] supportedTypes )
		{
			return DequeueAsync( supportedTypes )
				.Result;
		}

		public Task StartReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = true;
			return Task.Delay( 150 );
		}

		public Task StopReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = false;
			return Task.Delay( 150 );
		}

		public void WaitForQueueToBeDepleted()
		{
			mQueueDepletedHandle.Wait();
		}

		public void Dispose()
		{
			mProducedTasksBuffer.Clear();
			mDequeuedTasksHistory.Clear();
		}

		public ITimestampProvider TimestampProvider
			=> mTimeProvider;

		public List<IQueuedTaskToken> DequeuedTasksHistory
			=> mDequeuedTasksHistory;

		public bool IsReceivingNewTaskUpdates
			=> mIsReceivingNewTaskUpdates;

		public int DequeueCallCount
			=> mDequeueCallCount;
	}
}
