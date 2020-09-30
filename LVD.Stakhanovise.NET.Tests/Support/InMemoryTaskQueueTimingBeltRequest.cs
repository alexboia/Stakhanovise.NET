using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class InMemoryTaskQueueTimingBeltRequest
	{
		private TaskCompletionSource<AbstractTimestamp> mCompletionToken;

		private bool mIsDisposed;

		private long mRequestId;

		public InMemoryTaskQueueTimingBeltRequest ( long requestId,
			TaskCompletionSource<AbstractTimestamp> completionToken )
		{
			if ( requestId <= 0 )
				throw new ArgumentOutOfRangeException( nameof( requestId ),
					"Request ID must be greater than 0" );

			if ( completionToken == null )
				throw new ArgumentNullException( nameof( completionToken ) );

			mRequestId = requestId;
			mCompletionToken = completionToken;
		}

		public void SetCompleted ( AbstractTimestamp timestamp )
		{
			mCompletionToken.TrySetResult( timestamp );
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
					mCompletionToken = null;

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public long Id
			=> mRequestId;
	}
}
