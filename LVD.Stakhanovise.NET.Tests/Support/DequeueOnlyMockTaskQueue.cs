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
