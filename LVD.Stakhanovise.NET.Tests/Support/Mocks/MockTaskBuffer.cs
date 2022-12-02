using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockTaskBuffer : ITaskBuffer
	{
		private ITaskBuffer mInnerTaskBuffer;

		private int mRefusedElementCount = 0;

		public event EventHandler QueuedTaskRetrieved;
		public event EventHandler QueuedTaskAdded;

		public MockTaskBuffer( int capacity )
		{
			mInnerTaskBuffer = new StandardTaskBuffer( capacity, new StandardTaskBufferMetricsProvider() );
			mInnerTaskBuffer.QueuedTaskAdded += HandleInnerBufferTaskAdded;
			mInnerTaskBuffer.QueuedTaskRetrieved += HandleInnerBufferTaskRetrieved;
		}

		private void HandleInnerBufferTaskRetrieved( object sender, EventArgs e )
		{
			NotifyTaskRetrieved();
		}

		private void NotifyTaskRetrieved()
		{
			QueuedTaskRetrieved?.Invoke( this, EventArgs.Empty );
		}

		private void HandleInnerBufferTaskAdded( object sender, EventArgs e )
		{
			NotifyTaskAdded();
		}

		private void NotifyTaskAdded()
		{
			QueuedTaskAdded?.Invoke( this, EventArgs.Empty );
		}

		public void CompleteAdding()
		{
			mInnerTaskBuffer.CompleteAdding();
		}

		public void Dispose()
		{
			if ( mInnerTaskBuffer != null )
			{
				mInnerTaskBuffer.QueuedTaskAdded -= HandleInnerBufferTaskAdded;
				mInnerTaskBuffer.QueuedTaskRetrieved -= HandleInnerBufferTaskRetrieved;
				mInnerTaskBuffer.Dispose();
				mInnerTaskBuffer = null;
			}
		}

		public bool TryAddNewTask( IQueuedTaskToken task )
		{
			bool added = mInnerTaskBuffer.TryAddNewTask( task );
			if ( !added )
				mRefusedElementCount++;
			return added;
		}

		public IQueuedTaskToken TryGetNextTask()
		{
			return mInnerTaskBuffer.TryGetNextTask();
		}

		public void FakeNotifyQueuedTaskRetrieved()
		{
			NotifyTaskRetrieved();
		}

		public bool HasTasks => mInnerTaskBuffer.HasTasks;

		public bool IsFull => mInnerTaskBuffer.IsFull;

		public bool IsEmpty => !HasTasks;

		public int Capacity => mInnerTaskBuffer.Capacity;

		public int Count => mInnerTaskBuffer.Count;

		public bool IsCompleted => mInnerTaskBuffer.IsCompleted;

		public int RefusedElementCount => mRefusedElementCount;
	}
}
