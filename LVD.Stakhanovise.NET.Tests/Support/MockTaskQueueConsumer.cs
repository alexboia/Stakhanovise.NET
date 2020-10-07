using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockTaskQueueConsumer : ITaskQueueConsumer, IDisposable
	{
		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsReceivingNewTaskUpdates;

		private Queue<IQueuedTaskToken> mProducedTasksBuffer =
			new Queue<IQueuedTaskToken>();

		private List<IQueuedTaskToken> mDequeuedTasksHistory =
			new List<IQueuedTaskToken>();

		private Task<bool> mQueueDepletedHandle;

		private TaskCompletionSource<bool> mQueueDepletedTaskCompletionSource;

		private Task mGenerationCompletedTask = Task.CompletedTask;

		private int mNumberOfTasks;

		private int mRemainingTaskCount;

		private ITaskQueueAbstractTimeProvider mTimeProvider;

		public MockTaskQueueConsumer ( int numberOfTasks, ITaskQueueAbstractTimeProvider timeProvider )
		{
			//TODO: also add task type
			mQueueDepletedTaskCompletionSource = new TaskCompletionSource<bool>();
			mQueueDepletedHandle = mQueueDepletedTaskCompletionSource.Task;

			mNumberOfTasks = numberOfTasks;
			mRemainingTaskCount = numberOfTasks;
			mTimeProvider = timeProvider;
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

					mDequeuedTasksHistory.Add( new MockQueuedTaskToken( queuedTask, 
						new QueuedTaskResult( queuedTask ) ) );
					mProducedTasksBuffer.Enqueue( new MockQueuedTaskToken( queuedTask, 
						new QueuedTaskResult( queuedTask ) ) );

					NotifyClearToDequeue();
				}

				mRemainingTaskCount = Math.Max( mRemainingTaskCount - count, 0 );
			} );
		}

		public async Task<IQueuedTaskToken> DequeueAsync ( params string[] supportedTypes )
		{
			await mGenerationCompletedTask;

			if ( mProducedTasksBuffer.Count != 0 )
				return mProducedTasksBuffer.Dequeue();

			if ( mRemainingTaskCount <= 0 )
				mQueueDepletedTaskCompletionSource.TrySetResult( true );
			else
				mGenerationCompletedTask = GenerateNewTasksAsync();

			return null;
		}

		public IQueuedTaskToken Dequeue ( params string[] supportedTypes )
		{
			return DequeueAsync( supportedTypes ).Result;
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

		public void WaitForQueueToBeDepleted ()
		{
			mQueueDepletedHandle.Wait();
		}

		public void Dispose ()
		{
			mProducedTasksBuffer.Clear();
			mDequeuedTasksHistory.Clear();
		}

		public ITaskQueueAbstractTimeProvider TimeProvider
			=> mTimeProvider;

		public List<IQueuedTaskToken> DequeuedTasksHistory
			=> mDequeuedTasksHistory;

		public bool IsReceivingNewTaskUpdates
			=> mIsReceivingNewTaskUpdates;
	}
}
