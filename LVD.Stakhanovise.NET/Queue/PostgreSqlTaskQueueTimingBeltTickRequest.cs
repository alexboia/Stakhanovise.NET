using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueTimingBeltTickRequest : IDisposable
	{
		private CancellationToken mCancellationToken;

		private CancellationTokenSource mCancellationTokenSource;

		private TaskCompletionSource<AbstractTimestamp> mCompletionToken;

		private bool mIsDisposed;

		private int mCurrentFailCount = 0;

		private int mMaxFailCount;

		public PostgreSqlTaskQueueTimingBeltTickRequest ( TaskCompletionSource<AbstractTimestamp> completionToken,
			int timeoutMilliseconds,
			int maxFailCount )
		{
			mCancellationTokenSource =
				new CancellationTokenSource();

			if ( timeoutMilliseconds > 0 )
				mCancellationTokenSource.CancelAfter( timeoutMilliseconds );

			mCancellationToken = mCancellationTokenSource.Token;
			mCancellationToken.Register( () => completionToken.TrySetCanceled() );

			mCompletionToken = completionToken;
			mMaxFailCount = maxFailCount;
		}

		public void SetCompleted ( AbstractTimestamp timestamp )
		{
			if ( !mCancellationToken.IsCancellationRequested )
				mCompletionToken.TrySetResult( timestamp );
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
	}
}
