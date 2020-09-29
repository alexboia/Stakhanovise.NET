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
		public event EventHandler<TokenReleasedEventArgs> TokenReleased;

		public MockQueuedTaskToken ( IQueuedTask queuedTask )
		{
			QueuedTask = queuedTask;
			IsPending = true;
			IsLocked = true;
		}

		public MockQueuedTaskToken ( Guid queuedTaskId )
			: this( new QueuedTask( queuedTaskId ) )
		{
			return;
		}

		public Task ReleaseLockAsync ()
		{
			EventHandler<TokenReleasedEventArgs> h = TokenReleased;
			if ( h != null )
				h( this, new TokenReleasedEventArgs( QueuedTask.Id ) );

			IsLocked = false;
			IsActive = false;
			IsPending = false;

			return Task.CompletedTask;
		}

		public Task<bool> TrySetResultAsync ( TaskExecutionResult result )
		{
			IsActive = false;
			IsPending = false;
			IsLocked = false;
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
			return;
		}

		public IQueuedTask QueuedTask { get; private set; }

		public CancellationToken CancellationToken => CancellationToken.None;

		public bool IsPending { get; private set; }

		public bool IsActive { get; private set; }

		public bool IsLocked { get; private set; }
	}
}
