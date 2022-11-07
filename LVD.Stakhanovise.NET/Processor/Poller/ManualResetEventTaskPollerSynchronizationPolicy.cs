using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Processor
{
	public class ManualResetEventTaskPollerSynchronizationPolicy : ITaskPollerSynchronizationPolicy, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private readonly ITaskQueueConsumer mTaskQueueConsumer;

		private readonly ITaskBuffer mTaskBuffer;

		private readonly ITaskPollerMetricsProvider mMetricsProvider;

		private ManualResetEvent mWaitForClearToDequeue
			= new ManualResetEvent( initialState: false );

		private ManualResetEvent mWaitForClearToAddToBuffer
			= new ManualResetEvent( initialState: false );

		private bool mIsDisposed = false;

		public ManualResetEventTaskPollerSynchronizationPolicy( ITaskQueueConsumer taskQueueConsumer,
			ITaskBuffer taskBuffer,
			ITaskPollerMetricsProvider metricsProvider )
		{
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
		}

		public void NotifyPollerStarted()
		{
			CheckNotDisposedOrThrow();

			mTaskQueueConsumer.ClearForDequeue +=
				HandleClearForDequeueReceived;
			mTaskBuffer.QueuedTaskRetrieved +=
				HandleQueuedTaskRetrievedFromBuffer;

			Reset();
		}

		private void HandleQueuedTaskRetrievedFromBuffer( object sender, EventArgs e )
		{
			mLogger.Debug( "Received new buffer space available notification. Will resume polling..." );
			mWaitForClearToAddToBuffer.Set();
		}

		private void HandleClearForDequeueReceived( object sender, ClearForDequeueEventArgs e )
		{
			mLogger.DebugFormat( "Received poll for dequeue required notification from queue. Reason = {0}. Will resume polling...", e.Reason );
			mWaitForClearToDequeue.Set();
		}

		public void NotifyPollerStopRequested()
		{
			CheckNotDisposedOrThrow();

			//We may be waiting for the right conditions to
			//  try polling the queue (if the last poll yielded no task) 
			//	or for populating the buffer (if the buffer was full the last time we tried to post a task)
			//Thus, in order to avoid waiting for these conditions to be met 
			//  just to be able to stop we signal that processing can continue
			//  and the polling thread is responsible for double-checking that stopping 
			//  has not been requested in the mean-time

			mWaitForClearToDequeue.Set();
			mWaitForClearToAddToBuffer.Set();

			mTaskQueueConsumer.ClearForDequeue -=
				HandleClearForDequeueReceived;
			mTaskBuffer.QueuedTaskRetrieved -=
				HandleQueuedTaskRetrievedFromBuffer;
		}

		public void WaitForClearToAddToBuffer( CancellationToken cancellationToken )
		{
			CheckNotDisposedOrThrow();

			mWaitForClearToAddToBuffer.Reset();
			cancellationToken.ThrowIfCancellationRequested();

			mMetricsProvider.IncrementPollerWaitForBufferSpaceCount();

			mWaitForClearToAddToBuffer
				.WaitOne();
		}

		public void WaitForClearToDequeue( CancellationToken cancellationToken )
		{
			CheckNotDisposedOrThrow();

			mWaitForClearToDequeue.Reset();
			cancellationToken.ThrowIfCancellationRequested();

			mMetricsProvider.IncrementPollerWaitForDequeueCount();

			mWaitForClearToDequeue
				.WaitOne();
		}

		public void Reset()
		{
			CheckNotDisposedOrThrow();
			mWaitForClearToAddToBuffer.Reset();
			mWaitForClearToDequeue.Reset();
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				if ( !mIsDisposed )
				{
					mWaitForClearToDequeue.Dispose();
					mWaitForClearToDequeue = null;

					mWaitForClearToAddToBuffer.Dispose();
					mWaitForClearToAddToBuffer = null;

					mIsDisposed = true;
				}
			}
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( ManualResetEventTaskPollerSynchronizationPolicy ),
					"Cannot reuse a disposed task poller synchronization policy"
				);
			}
		}
	}
}
