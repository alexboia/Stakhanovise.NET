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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockTaskQueueConsumer : ITaskQueueConsumer, IDisposable
	{
		private const int TaskGenerationTimeoutMilliseconds = 75;

		private const int MaxSizeOfGeneratedTasksBatch = 2;

		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsReceivingNewTaskUpdates = false;

		private readonly ConcurrentQueue<IQueuedTaskToken> mProducedTasksBuffer =
			new ConcurrentQueue<IQueuedTaskToken>();

		private readonly ConcurrentBag<IQueuedTaskToken> mDequeuedTasksHistory =
			new ConcurrentBag<IQueuedTaskToken>();

		private ManualResetEvent mQueueDepletedHandle;

		private Task mGenerationCompletedTask = Task.CompletedTask;

		private int mRemainingTaskCount;

		private int mDequeueCallCount = 0;

		private int mActuallyDequeuedElementsCount = 0;

		public MockTaskQueueConsumer( int numberOfTasksToGenerate )
		{
			mQueueDepletedHandle = new ManualResetEvent( false );
			mRemainingTaskCount = numberOfTasksToGenerate;
		}

		private void NotifyClearToDequeue()
		{
			EventHandler<ClearForDequeueEventArgs> handler = ClearForDequeue;
			if ( handler != null )
				handler( this, new ClearForDequeueEventArgs( ClearForDequeReason.NewTaskPostedNotificationReceived ) );
		}

		public async Task<IQueuedTaskToken> DequeueAsync( params string [] supportedTypes )
		{
			IncrementDequeueCallCount();
			await WaitForTaskGenerationCompletionAsync();

			if ( HasAlreadyProducedTasks() )
			{
				IncrementActuallyDequeuedElementsCount();
				return FetchAndRemoveProducedTask();
			}

			if ( HasAnyMoreTasksToGenerate() )
				StartGeneratingNewTasks();
			else
				SetTaskQueueDepleted();

			return null;
		}

		private void IncrementDequeueCallCount()
		{
			Interlocked.Increment( ref mDequeueCallCount );
		}

		private void IncrementActuallyDequeuedElementsCount()
		{
			Interlocked.Increment( ref mActuallyDequeuedElementsCount );
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
			if ( !mProducedTasksBuffer.TryDequeue( out IQueuedTaskToken queuedTaskToken ) )
				queuedTaskToken = null;
			return queuedTaskToken;
		}

		private bool HasAnyMoreTasksToGenerate()
		{
			return mRemainingTaskCount > 0;
		}

		private void StartGeneratingNewTasks()
		{
			if ( IsCurrentlyGeneratingQueuedTasks() )
				throw new InvalidOperationException( "Attempted to start generation while another task generation operation is running" );

			mGenerationCompletedTask = 
				GenerateNewTasksAsync();
		}

		private bool IsCurrentlyGeneratingQueuedTasks()
		{
			return mGenerationCompletedTask.Status != TaskStatus.RanToCompletion
				&& mGenerationCompletedTask.Status != TaskStatus.Canceled
				&& mGenerationCompletedTask.Status != TaskStatus.Faulted;
		}

		private Task GenerateNewTasksAsync()
		{
			return Task.Run( () =>
			{
				Task.Delay( TaskGenerationTimeoutMilliseconds )
					.Wait();

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

		private void SetTaskQueueDepleted()
		{
			mQueueDepletedHandle.Set();
		}

		public IQueuedTaskToken Dequeue( params string [] supportedTypes )
		{
			return DequeueAsync( supportedTypes )
				.Result;
		}

		public Task StartReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = true;
			return Task.CompletedTask;
		}

		public Task StopReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = false;
			return Task.CompletedTask;
		}

		public void WaitForQueueToBeDepleted()
		{
			mQueueDepletedHandle.WaitOne();
		}

		public void Dispose()
		{
			mProducedTasksBuffer.Clear();
			mDequeuedTasksHistory.Clear();

			mQueueDepletedHandle.Dispose();
			mQueueDepletedHandle = null;

			mGenerationCompletedTask = null;
		}

		public List<IQueuedTaskToken> DequeuedTasksHistory
			=> mDequeuedTasksHistory.ToList();

		public bool IsReceivingNewTaskUpdates
			=> mIsReceivingNewTaskUpdates;

		public int DequeueCallCount
			=> mDequeueCallCount;

		public int ActuallyDequeuedElementsCount
			=> mActuallyDequeuedElementsCount;
	}
}
