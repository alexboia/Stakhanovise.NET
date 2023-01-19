using LVD.Stakhanovise.NET.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Model
{
	public class AsyncProcessingRequestBatchProcessor<TRequest> : IDisposable
		where TRequest : IAsyncProcessingRequest
	{
		private const int ProcessingBatchSize = 5;

		private readonly Func<AsyncProcessingRequestBatch<TRequest>, Task> mRequestBatchProcessingDelegate;

		private readonly IStakhanoviseLogger mLogger;

		private CancellationTokenSource mStopCoordinator;

		private BlockingCollection<TRequest> mProcessingQueue;

		private Task mProcessingTask;

		private StateController mStateController = new StateController();

		private bool mIsDisposed;

		public AsyncProcessingRequestBatchProcessor( Func<AsyncProcessingRequestBatch<TRequest>, Task> requestBatchProcessingDelegate,
			IStakhanoviseLogger logger )
		{
			mRequestBatchProcessingDelegate = requestBatchProcessingDelegate
				?? throw new ArgumentNullException( nameof( requestBatchProcessingDelegate ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( AsyncProcessingRequestBatchProcessor<TRequest> ),
					"Cannot reuse a disposed object"
				);
			}
		}

		private void CheckRunningOrThrow()
		{
			if ( !IsRunning )
				throw new InvalidOperationException( "The async request processor is not running." );
		}

		public Task PostRequestAsync( TRequest request )
		{
			CheckNotDisposedOrThrow();
			CheckRunningOrThrow();
			mProcessingQueue.Add( request );
			return Task.CompletedTask;
		}

		public Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStopped )
				mStateController.TryRequestStart( StartProcessing );

			return Task.CompletedTask;
		}

		private void StartProcessing()
		{
			mStopCoordinator = new CancellationTokenSource();
			mProcessingQueue = new BlockingCollection<TRequest>();
			mProcessingTask = Task.Run( RunProcessingLoopAsync );
		}

		private async Task RunProcessingLoopAsync()
		{
			CancellationToken stopToken = mStopCoordinator
				.Token;

			while ( !stopToken.IsCancellationRequested )
			{
				try
				{
					await ProcessNextBatchOfRequestsAsync( stopToken, readToEnd: false );
					stopToken.ThrowIfCancellationRequested();
				}
				catch ( OperationCanceledException )
				{
					break;
				}
			}

			await ProcessNextBatchOfRequestsAsync( stopToken, readToEnd: true );
		}

		private async Task ProcessNextBatchOfRequestsAsync( CancellationToken stopToken, bool readToEnd )
		{
			//We need to use a queue here - as we process the batch, 
			//	we consume each element and, in case of an error 
			//	that affects all of them, 
			//	we would fail only the remaining ones, not the ones 
			//	that have been successfully processed
			AsyncProcessingRequestBatch<TRequest> nextBatch = null;

			try
			{
				nextBatch = ExtractNextBatchOfRequests( stopToken, readToEnd );
				await ProcessRequestBatchAsync( nextBatch );
			}
			catch ( Exception exc )
			{
				//Add them back to processing queue to be retried
				if ( nextBatch != null )
				{
					foreach ( TRequest rq in nextBatch )
					{
						rq.SetFailed( exc );
						if ( rq.CanBeRetried && !mProcessingQueue.IsAddingCompleted )
							mProcessingQueue.Add( rq );
					}
				}

				mLogger.Error( "Error processing results",
					exc );
			}
			finally
			{
				nextBatch?.Clear();
			}
		}

		private AsyncProcessingRequestBatch<TRequest> ExtractNextBatchOfRequests( CancellationToken stopToken, bool readToEnd )
		{
			int batchSize = readToEnd
				? mProcessingQueue.Count
				: ProcessingBatchSize;

			AsyncProcessingRequestBatch<TRequest> nextBatch =
				new AsyncProcessingRequestBatch<TRequest>( batchSize );

			nextBatch.FillFrom( mProcessingQueue,
				stopToken );

			return nextBatch;
		}

		private async Task ProcessRequestBatchAsync( AsyncProcessingRequestBatch<TRequest> currentBatch )
		{
			await mRequestBatchProcessingDelegate.Invoke( currentBatch );
			foreach ( TRequest rq in currentBatch )
			{
				if ( !rq.IsCompleted 
					&& rq.CurrentFailCount > 0 
					&& rq.CanBeRetried 
					&& !mProcessingQueue.IsAddingCompleted )
					mProcessingQueue.Add( rq );
			}
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopAsync( StopProcessingAsync );
		}

		private async Task StopProcessingAsync()
		{
			mProcessingQueue.CompleteAdding();
			mStopCoordinator.Cancel();
			await mProcessingTask;

			mProcessingQueue.Dispose();
			mStopCoordinator.Dispose();

			mProcessingQueue = null;
			mStopCoordinator = null;
			mProcessingTask = null;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					mStateController = null;
				}

				mIsDisposed = true;
			}
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
