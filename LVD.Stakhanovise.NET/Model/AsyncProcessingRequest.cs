using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Model
{
	public class AsyncProcessingRequest<TResult> : IDisposable
	{
		private CancellationToken mCancellationToken;

		private CancellationTokenRegistration mCancellationTokenRegistration;

		private CancellationTokenSource mCancellationTokenSource;

		private TaskCompletionSource<TResult> mCompletionToken;

		private bool mIsDisposed;

		private int mCurrentFailCount = 0;

		private int mMaxFailCount;

		private long mRequestId;

		public AsyncProcessingRequest ( long requestId,
			TaskCompletionSource<TResult> completionToken,
			int timeoutMilliseconds,
			int maxFailCount )
		{
			if ( requestId <= 0 )
				throw new ArgumentOutOfRangeException( nameof( requestId ),
					"Request ID must be greater than 0" );

			if ( completionToken == null )
				throw new ArgumentNullException( nameof( completionToken ) );

			if ( maxFailCount < 0 )
				throw new ArgumentOutOfRangeException( nameof( maxFailCount ),
					"Maximum allowed fail count must be greater than or equal to 0" );

			mRequestId = requestId;
			mCancellationTokenSource = new CancellationTokenSource();

			//If timeout is specified, then schedule the CTS 
			//	to automatically request cancellation
			if ( timeoutMilliseconds > 0 )
				mCancellationTokenSource.CancelAfter( timeoutMilliseconds );

			//Register a handler for when cancellation is requested
			mCancellationToken = mCancellationTokenSource.Token;
			mCancellationTokenRegistration = mCancellationToken.Register( () => HandleCancellationRequested() );

			mCompletionToken = completionToken;
			mMaxFailCount = maxFailCount;
		}

		private void HandleCancellationRequested ()
		{
			mCompletionToken.TrySetCanceled();
		}

		public void SetCancelled ()
		{
			if ( !mCancellationToken.IsCancellationRequested )
				mCancellationTokenSource.Cancel();
		}

		public void SetCompleted ( TResult result )
		{
			if ( !mCancellationToken.IsCancellationRequested )
				mCompletionToken.TrySetResult( result );
		}

		public void SetFailed ( Exception exc )
		{
			if ( !mCancellationToken.IsCancellationRequested )
			{
				IncrementFailCount();
				if ( !CanBeRetried )
					mCompletionToken.TrySetException( exc );
			}
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					mCancellationTokenRegistration.Dispose();
					mCancellationTokenSource.Dispose();
					mCancellationTokenSource = null;
					mCompletionToken = null;
				}

				mIsDisposed = true;
			}
		}

		private void IncrementFailCount ()
		{
			Interlocked.Increment( ref mCurrentFailCount );
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public bool CanBeRetried
			=> mCurrentFailCount < mMaxFailCount;

		public long Id
			=> mRequestId;
	}
}
