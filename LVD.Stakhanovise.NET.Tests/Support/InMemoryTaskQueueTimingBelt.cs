using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class InMemoryTaskQueueTimingBelt : ITaskQueueTimingBelt, IDisposable
	{
		private AbstractTimestamp mLastTime;

		private bool mIsDisposed = false;

		private bool mIsRunning = false;

		private long mLastRequestId = 0;

		private long mTotalLocalWallclockTimeCost;

		private BlockingCollection<InMemoryTaskQueueTimingBeltRequest> mTickingQueue;

		private CancellationTokenSource mTimeTickingStopRequest;

		private Task mTimeTickingTask;

		public InMemoryTaskQueueTimingBelt ( long initialWallclockTimeCost )
		{
			mTotalLocalWallclockTimeCost = initialWallclockTimeCost;
			mLastTime = new AbstractTimestamp( 0, initialWallclockTimeCost );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( InMemoryTaskQueueTimingBelt ),
					"Cannot reuse a disposed in-memory task queue timing belt" );
		}

		private void CheckRunningOrThrow ()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The timing belt is not running." );
		}

		private void StartTimeTickingTask ()
		{
			mTimeTickingStopRequest = new CancellationTokenSource();
			mTickingQueue = new BlockingCollection<InMemoryTaskQueueTimingBeltRequest>();

			mTimeTickingTask = Task.Run( () =>
			{
				CancellationToken stopToken = mTimeTickingStopRequest
				   .Token;

				while ( true )
				{
					try
					{
						stopToken.ThrowIfCancellationRequested();

						InMemoryTaskQueueTimingBeltRequest request = mTickingQueue
							.Take( stopToken );

						AbstractTimestamp lastTime = mLastTime
							.AddTicks( 1 );

						request.SetCompleted( lastTime.Copy() );
						mLastTime = lastTime.Copy();
					}
					catch ( OperationCanceledException )
					{
						break;
					}
				}
			} );
		}

		private async Task StopTimeTickingTaskAsync ()
		{
			mTickingQueue.CompleteAdding();
			mTimeTickingStopRequest.Cancel();
			await mTimeTickingTask;

			mTickingQueue.Dispose();
			mTimeTickingStopRequest.Dispose();

			mTimeTickingStopRequest = null;
			mTickingQueue = null;
			mTimeTickingTask = null;
		}

		public void AddWallclockTimeCost ( long milliseconds )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			Interlocked.Add( ref mTotalLocalWallclockTimeCost,
				milliseconds );
		}

		public void AddWallclockTimeCost ( TimeSpan duration )
		{
			AddWallclockTimeCost( ( long )duration.TotalMilliseconds );
		}

		public Task<long> ComputeAbsoluteTimeTicksAsync ( long timeTicksToAdd )
		{
			CheckNotDisposedOrThrow();
			return Task.FromResult( mLastTime.Ticks + timeTicksToAdd );
		}

		public Task<AbstractTimestamp> GetCurrentTimeAsync ()
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			return Task.FromResult( mLastTime.Copy() );
		}

		public Task StartAsync ()
		{
			if ( !mIsRunning )
			{
				mIsRunning = true;
				StartTimeTickingTask();
			}

			return Task.CompletedTask;
		}

		public async Task StopAsync ()
		{
			if ( mIsRunning )
			{
				mIsRunning = false;
				await StopTimeTickingTaskAsync();
			}
		}

		public Task<AbstractTimestamp> TickAbstractTimeAsync ( int timeout )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();

			long requestId = Interlocked.Increment( ref mLastRequestId );
			AbstractTimestamp lastTime = mLastTime.Copy();

			TaskCompletionSource<AbstractTimestamp> completionToken =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions
					.RunContinuationsAsynchronously );

			InMemoryTaskQueueTimingBeltRequest tickRequest =
				new InMemoryTaskQueueTimingBeltRequest( requestId,
					completionToken );

			mTickingQueue.Add( tickRequest );

			return completionToken.Task.WithCleanup( ( prev )
				=> tickRequest.Dispose() );
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					mLastTime = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public AbstractTimestamp LastTime
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mLastTime.Copy();
			}
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mIsRunning;
			}
		}
	}
}
