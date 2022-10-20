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

		private ITaskQueueConsumer mTaskQueueConsumer;

		private ITaskBuffer mTaskBuffer;

		private ManualResetEvent mWaitForClearToDequeue
			= new ManualResetEvent( initialState: false );

		private ManualResetEvent mWaitForClearToAddToBuffer
			= new ManualResetEvent( initialState: false );

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.PollerWaitForBufferSpaceCount,
			AppMetricId.PollerWaitForDequeueCount
		);

		private bool mIsDisposed = false;

		public ManualResetEventTaskPollerSynchronizationPolicy( ITaskQueueConsumer taskQueueConsumer, ITaskBuffer taskBuffer )
		{
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
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

			mMetrics.UpdateMetric( AppMetricId.PollerWaitForBufferSpaceCount,
				m => m.Increment() );

			mWaitForClearToAddToBuffer
				.WaitOne();
		}

		public void WaitForClearToDequeue( CancellationToken cancellationToken )
		{
			CheckNotDisposedOrThrow();

			mWaitForClearToDequeue.Reset();
			cancellationToken.ThrowIfCancellationRequested();

			mMetrics.UpdateMetric( AppMetricId.PollerWaitForDequeueCount,
				m => m.Increment() );

			mWaitForClearToDequeue
				.WaitOne();
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public void Reset()
		{
			CheckNotDisposedOrThrow();
			mWaitForClearToAddToBuffer.Reset();
			mWaitForClearToDequeue.Reset();
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

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public IEnumerable<IAppMetricId> ExportedMetrics
			=> mMetrics.ExportedMetrics;
	}
}
