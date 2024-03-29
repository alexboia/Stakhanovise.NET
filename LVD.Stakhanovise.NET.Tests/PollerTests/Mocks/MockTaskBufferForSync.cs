﻿using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using MoreLinq;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Tests.PollerTests.Mocks
{
	public class MockTaskBufferForSync : ITaskBuffer
	{
		public event EventHandler QueuedTaskRetrieved;

		public event EventHandler QueuedTaskAdded;

		private int mCapacity;

		private bool mCompleted;

		private Queue<IQueuedTaskToken> mQueuedTasks = new Queue<IQueuedTaskToken>();

		public MockTaskBufferForSync()
			: this( 1 )
		{
			return;
		}

		public MockTaskBufferForSync( int capacity )
		{
			mCapacity = capacity;
		}

		public void BeginAdding()
		{
			mCompleted = false;
		}

		public void CompleteAdding()
		{
			mCompleted = true;
		}

		public void FillWithMocksToMaxCapacity()
		{
			while ( !IsFull )
				mQueuedTasks.Enqueue( new MockQueuedTaskToken( Guid.NewGuid() ) );
		}

		public bool TryAddNewTask( IQueuedTaskToken taskToken )
		{
			if ( mQueuedTasks.Count < mCapacity )
			{
				mQueuedTasks.Enqueue( taskToken );
				TriggerQueuedTaskTaskAdded();
				return true;
			}
			else
				return false;
		}

		public void TriggerQueuedTaskTaskAdded()
		{
			EventHandler handler = QueuedTaskAdded;
			if ( handler != null )
				handler( this, EventArgs.Empty );
		}

		public IQueuedTaskToken TryGetNextTask()
		{
			if ( mQueuedTasks.TryDequeue( out IQueuedTaskToken taskToken ) )
			{
				TriggerQueuedTaskTaskRetrieved();
				return taskToken;
			}
			else
				return null;
		}

		public void TriggerQueuedTaskTaskRetrieved()
		{
			EventHandler handler = QueuedTaskRetrieved;
			if ( handler != null )
				handler( this, EventArgs.Empty );
		}

		public void Dispose()
		{
			return;
		}

		private int GetInvocationListCount( EventHandler handler )
		{
			return handler != null
				? handler.GetInvocationList().Length
				: 0;
		}

		public int QueuedTaskAddedHandlerCount => GetInvocationListCount( QueuedTaskAdded );

		public int QueuedTaskRetrievedHandlerCount => GetInvocationListCount( QueuedTaskRetrieved );

		public bool HasTasks => Count > 0;

		public bool IsFull => Count == Capacity;

		public int Capacity => mCapacity;

		public int Count => mQueuedTasks.Count;

		public bool IsCompleted => mCompleted;
	}
}
