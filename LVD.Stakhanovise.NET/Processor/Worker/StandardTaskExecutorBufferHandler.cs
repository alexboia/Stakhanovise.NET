using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutorBufferHandler : ITaskExecutorBufferHandler, IDisposable
	{
		private ITaskBuffer mTaskBuffer;

		private ITaskExecutionMetricsProvider mMetricsProvider;

		private IStakhanoviseLogger mLogger;

		private CancellationTokenRegistration mCancellationTokenRegistration;

		private ManualResetEvent mWaitForBufferContents = new ManualResetEvent( false );

		private bool mIsDisposed = false;

		public StandardTaskExecutorBufferHandler( ITaskBuffer taskBuffer,
			ITaskExecutionMetricsProvider metricsProvider,
			CancellationToken cancellationToken,
			IStakhanoviseLogger logger )
		{
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );

			SetUpBufferMonitoring( cancellationToken );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardTaskExecutorBufferHandler ),
					"Cannot reuse a disposed task execution buffer handler"
				);
			}
		}

		private void SetUpBufferMonitoring( CancellationToken cancellationToken )
		{
			if ( cancellationToken == CancellationToken.None )
				throw new ArgumentNullException( nameof( cancellationToken ) );

			if ( cancellationToken.IsCancellationRequested )
				return;

			mCancellationTokenRegistration = cancellationToken
				.Register( TearDownBufferMonitoring );
			mTaskBuffer.QueuedTaskAdded
				+= HandleQueuedTaskAdded;
		}

		private void HandleQueuedTaskAdded( object sender, EventArgs e )
		{
			mWaitForBufferContents.Set();
		}

		private void TearDownBufferMonitoring()
		{
			//We may be waiting for the right conditions to
			//  try polling the buffer again
			//Thus, in order to avoid waiting for these conditions to be met 
			//  just to be able to stop we signal that processing can continue
			//  and the polling thread is responsible for double-checking that stopping 
			//  has not been requested in the mean-time
			mWaitForBufferContents
				.Set();
			mTaskBuffer.QueuedTaskAdded
				-= HandleQueuedTaskAdded;
			mCancellationTokenRegistration
				.Dispose();
		}

		public void WaitForTaskAvailability()
		{
			CheckNotDisposedOrThrow();

			//Buffer has tasks, all good
			if ( mTaskBuffer.HasTasks )
				return;

			//No tasks found in buffer, dig deeper
			mLogger.Debug( "No tasks found. Checking if buffer is completed..." );

			//It may be that it ran out of tasks because 
			//  it was marked as completed and all 
			//  the remaining tasks were consumed
			//In this case, waiting would mean waiting for ever, 
			//  since a completed buffer will no longer have 
			//  any items added to it
			if ( mTaskBuffer.IsCompleted )
			{
				mLogger.Debug( "Buffer completed, will break worker processing loop." );
				return;
			}

			//TODO: if there are no more buffer items AND the buffer is set as completed, 
			//	it may take some time until we notice (and via other channels)
			//Wait for tasks to become available
			mMetricsProvider.IncrementBufferWaitCount();
			mWaitForBufferContents.WaitOne();
		}

		public IQueuedTaskToken TryGetNextTask()
		{
			CheckNotDisposedOrThrow();

			IQueuedTaskToken nextTask = mTaskBuffer.TryGetNextTask();
			mWaitForBufferContents.Reset();

			return nextTask;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					mWaitForBufferContents.Dispose();
					mWaitForBufferContents = null;
					mMetricsProvider = null;
					mTaskBuffer = null;
					mLogger = null;
				}

				mIsDisposed = true;
			}
		}
	}
}
