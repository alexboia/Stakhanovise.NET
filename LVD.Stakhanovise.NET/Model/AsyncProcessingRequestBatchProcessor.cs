// 
// BSD 3-Clause License
// 
// Copyright (c) 2026, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using LVD.Stakhanovise.NET.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Model
{
	public class AsyncProcessingRequestBatchProcessor<TRequest> : IDisposable, IAsyncDisposable
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
			if (mIsDisposed)
			{
				throw new ObjectDisposedException(
					nameof( AsyncProcessingRequestBatchProcessor<TRequest> ),
					"Cannot reuse a disposed object"
				);
			}
		}

		private void CheckRunningOrThrow()
		{
			if (!IsRunning)
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

			TaskCompletionSource<bool> startedCompletionSource =
				new TaskCompletionSource<bool>( TaskCreationOptions
					.RunContinuationsAsynchronously );

			if (mStateController.IsStopped)
				mStateController.TryRequestStart( () => StartProcessing( startedCompletionSource ) );
			else
				startedCompletionSource.TrySetResult( true );

			return startedCompletionSource.Task;
		}

		private void StartProcessing( TaskCompletionSource<bool> startedCompletionSource )
		{
			try
			{
				mStopCoordinator = new CancellationTokenSource();
				mProcessingQueue = new BlockingCollection<TRequest>();
				mProcessingTask = Task.Run( () => RunProcessingLoopAsync( startedCompletionSource ) );
			}
			catch (Exception exc)
			{
				startedCompletionSource.TrySetException( exc );
				throw;
			}
		}

		private async Task RunProcessingLoopAsync( TaskCompletionSource<bool> startedCompletionSource )
		{
			CancellationToken stopToken = mStopCoordinator.Token;

			startedCompletionSource.SetResult( !stopToken.IsCancellationRequested );
			while (!stopToken.IsCancellationRequested)
			{
				try
				{
					await ProcessNextBatchOfRequestsAsync( stopToken, readToEnd: false )
						.ConfigureAwait( false );

					stopToken.ThrowIfCancellationRequested();
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}

			await ProcessNextBatchOfRequestsAsync( stopToken, readToEnd: true )
				.ConfigureAwait( false );
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
				await ProcessRequestBatchAsync( nextBatch ).ConfigureAwait( false );
			}
			catch (Exception exc)
			{
				//Add them back to processing queue to be retried
				if (nextBatch != null)
				{
					foreach (TRequest rq in nextBatch)
					{
						rq.SetFailed( exc );
						if (rq.CanBeRetried && !mProcessingQueue.IsAddingCompleted)
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
			await mRequestBatchProcessingDelegate
				.Invoke( currentBatch )
				.ConfigureAwait( false );

			foreach (TRequest rq in currentBatch)
			{
				if (!rq.IsCompleted
					&& rq.CurrentFailCount > 0
					&& rq.CanBeRetried
					&& !mProcessingQueue.IsAddingCompleted)
					mProcessingQueue.Add( rq );
			}
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if (mStateController.IsStarted)
			{
				await mStateController
					.TryRequestStopAsync( StopProcessingAsync )
					.ConfigureAwait( false );
			}
		}

		private async Task StopProcessingAsync()
		{
			if (mProcessingQueue == null || mStopCoordinator == null || mProcessingTask == null)
				return;

			mProcessingQueue.CompleteAdding();
			mStopCoordinator.Cancel();
			await mProcessingTask.ConfigureAwait( false );

			mProcessingQueue.Dispose();
			mStopCoordinator.Dispose();

			mProcessingQueue = null;
			mStopCoordinator = null;
			mProcessingTask = null;
		}

		public void Dispose()
		{
			if (mIsDisposed)
				return;

			DisposeAsync().ConfigureAwait( false ).GetAwaiter().GetResult();
			GC.SuppressFinalize( this );
		}

		public async ValueTask DisposeAsync()
		{
			if (mIsDisposed)
				return;

			await DisposeAsyncCore().ConfigureAwait( false );
			GC.SuppressFinalize( this );
		}

		protected virtual async ValueTask DisposeAsyncCore()
		{
			if (!mIsDisposed)
			{
				await StopAsync().ConfigureAwait( false );
				mStateController = null;
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
