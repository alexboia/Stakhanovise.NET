using LVD.Stakhanovise.NET.Queue;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.PollerTests.Mocks
{
	public class MockTaskQueueConsumerForSync : ITaskQueueConsumer
	{
		public event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		private bool mIsReceivingNewTaskUpdates;

		public IQueuedTaskToken Dequeue( params string [] supportedTypes )
		{
			return null;
		}

		public Task<IQueuedTaskToken> DequeueAsync( params string [] supportedTypes )
		{
			return Task.FromResult<IQueuedTaskToken>( Dequeue( supportedTypes ) );
		}

		public Task StartReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = true;
			return Task.CompletedTask;
		}

		public void TriggerClearForDequeue( ClearForDequeReason reason )
		{
			EventHandler<ClearForDequeueEventArgs> handler = ClearForDequeue;
			if ( handler != null )
				handler( this, new ClearForDequeueEventArgs( reason ) );
		}

		public Task StopReceivingNewTaskUpdatesAsync()
		{
			mIsReceivingNewTaskUpdates = false;
			return Task.CompletedTask;
		}

		private int GetInvocationList( EventHandler<ClearForDequeueEventArgs> handler )
		{
			return handler != null
				? handler.GetInvocationList().Length
				: 0;
		}

		public int ClearForDequeueCount => GetInvocationList( ClearForDequeue );

		public bool IsReceivingNewTaskUpdates => mIsReceivingNewTaskUpdates;
	}
}
