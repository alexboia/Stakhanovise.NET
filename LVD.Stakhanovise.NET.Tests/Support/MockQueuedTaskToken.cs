using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockQueuedTaskToken : IQueuedTaskToken
	{
		private QueuedTask mQueuedTask;
		
		public event EventHandler<TokenReleasedEventArgs> TokenReleased;

		public MockQueuedTaskToken ( QueuedTask queuedTask )
		{
			mQueuedTask = queuedTask;
			IsPending = true;
			IsLocked = true;
		}

		public MockQueuedTaskToken ( Guid queuedTaskId )
			: this( new QueuedTask( queuedTaskId ) )
		{
			return;
		}

		private void NotityTokenReleased()
		{
			EventHandler<TokenReleasedEventArgs> h = TokenReleased;
			if ( h != null )
				h( this, new TokenReleasedEventArgs( DequeuedTask.Id ) );
		}

		public Task ReleaseLockAsync ()
		{
			IsLocked = false;
			IsActive = false;
			IsPending = false;

			NotityTokenReleased();
			return Task.CompletedTask;
		}

		public Task<bool> TrySetResultAsync ( TaskExecutionResult result )
		{
			IsActive = false;
			IsPending = false;
			IsLocked = false;

			if ( !result.ExecutedSuccessfully )
				mQueuedTask.Status = QueuedTaskStatus.Error;
			else
				mQueuedTask.Status = QueuedTaskStatus.Processed;

			NotityTokenReleased();
			return Task.FromResult( true );
		}

		public Task<bool> TrySetStartedAsync ( long estimatedProcessingTimeMillisencods )
		{
			IsPending = false;
			IsActive = true;
			return Task.FromResult( true );
		}

		public void Dispose ()
		{
			TokenReleased = null;
		}

		public IQueuedTask DequeuedTask => mQueuedTask;

		public CancellationToken CancellationToken => CancellationToken.None;

		public AbstractTimestamp DequeuedAt => AbstractTimestamp.Zero();

		public bool IsPending { get; private set; }

		public bool IsActive { get; private set; }

		public bool IsLocked { get; private set; }
	}
}
