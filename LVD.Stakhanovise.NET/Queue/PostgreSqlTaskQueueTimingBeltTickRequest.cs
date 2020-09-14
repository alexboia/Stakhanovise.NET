using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueTimingBeltTickRequest : IDisposable
	{
		public const int STATUS_COMPLETED = 0x01;

		public const int STATUS_PENDING = 0x02;

		public const int STATUS_CANCELLED = 0x02;

		private CancellationToken mCancellationToken;

		private CancellationTokenSource mCancellationTokenSource;

		private TaskCompletionSource<AbstractTimestamp> mCompletionToken;

		private bool mIsDisposed;

		private int mCurrentFailCount = 0;

		private int mMaxFailCount;

		private int mStatus = STATUS_PENDING;

		private long mRequestId;

		public PostgreSqlTaskQueueTimingBeltTickRequest ( long requestId, 
			TaskCompletionSource<AbstractTimestamp> completionToken,
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

			if ( timeoutMilliseconds > 0 )
				mCancellationTokenSource.CancelAfter( timeoutMilliseconds );

			mCancellationToken = mCancellationTokenSource.Token;
			mCancellationToken.Register( () => HandleCancellationRequested() );

			mCompletionToken = completionToken;
			mMaxFailCount = maxFailCount;
		}

		private bool TrySetCancelledStatus ()
		{
			return Interlocked.CompareExchange( ref mStatus, STATUS_CANCELLED, STATUS_PENDING )
				== STATUS_PENDING;
		}

		private bool TrySetCompletedStatus ()
		{
			return Interlocked.CompareExchange( ref mStatus, STATUS_COMPLETED, STATUS_PENDING )
				== STATUS_PENDING;
		}

		private void HandleCancellationRequested ()
		{
			if ( TrySetCancelledStatus() )
				mCompletionToken.TrySetCanceled();
		}

		public void SetCompleted ( AbstractTimestamp timestamp )
		{
			if ( !mCancellationToken.IsCancellationRequested && TrySetCompletedStatus() )
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
				{
					if ( TrySetCompletedStatus() )
						mCompletionToken.TrySetException( exc );
				}
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

		public bool CanBeRetried
			=> mCurrentFailCount < mMaxFailCount;

		public long Id
			=> mRequestId;
	}
}
