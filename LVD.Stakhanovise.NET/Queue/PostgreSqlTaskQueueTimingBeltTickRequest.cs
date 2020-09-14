using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueTimingBeltTickRequest : IDisposable
	{
		private const int COMPLETED = 0x01;

		private const int PENDING = 0x02;

		private const int CANCELLED = 0x02;

		private CancellationToken mCancellationToken;

		private CancellationTokenSource mCancellationTokenSource;

		private TaskCompletionSource<AbstractTimestamp> mCompletionToken;

		private bool mIsDisposed;

		private int mCurrentFailCount = 0;

		private int mMaxFailCount;

		private int mStatus = PENDING;

		private long mRequestId;

		public PostgreSqlTaskQueueTimingBeltTickRequest ( long requestId, TaskCompletionSource<AbstractTimestamp> completionToken,
			int timeoutMilliseconds,
			int maxFailCount )
		{
			mRequestId = requestId;
			mCancellationTokenSource = new CancellationTokenSource();

			if ( timeoutMilliseconds > 0 )
				mCancellationTokenSource.CancelAfter( timeoutMilliseconds );

			mCancellationToken = mCancellationTokenSource.Token;
			mCancellationToken.Register( () =>
			{
				Console.WriteLine( "Request with id {0} requested to cancel", mRequestId );
				if ( Interlocked.CompareExchange( ref mStatus, CANCELLED, PENDING ) == PENDING )
					completionToken.TrySetCanceled();
			} );

			mCompletionToken = completionToken;
			mMaxFailCount = maxFailCount;
		}

		public void SetCompleted ( AbstractTimestamp timestamp )
		{
			if ( !mCancellationToken.IsCancellationRequested )
			{
				if ( Interlocked.CompareExchange( ref mStatus, COMPLETED, PENDING ) == PENDING )
					mCompletionToken.TrySetResult( timestamp );
			}

		}

		public void SetCancelled ()
		{
			if ( !mCancellationToken.IsCancellationRequested )
				mCancellationTokenSource.Cancel();
		}

		public void SetFailed ( Exception exc )
		{
			if ( !mCancellationToken.IsCancellationRequested )
			{
				if ( !CanBeRetried )
					mCompletionToken.TrySetException( exc );
				else
					IncrementFailCount();
			}
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
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

		public bool CanBeRetried => mCurrentFailCount < mMaxFailCount;

		public long Id => mRequestId;
	}
}
