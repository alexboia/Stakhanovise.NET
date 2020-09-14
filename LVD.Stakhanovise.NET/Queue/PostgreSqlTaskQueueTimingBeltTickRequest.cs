// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
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

			//If timeout is specified, then schedule the CTS 
			//	to automatically request cancellation
			if ( timeoutMilliseconds > 0 )
				mCancellationTokenSource.CancelAfter( timeoutMilliseconds );

			//Register a handler for when cancellation is requested
			mCancellationToken = mCancellationTokenSource.Token;
			mCancellationToken.Register( () => HandleCancellationRequested() );

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

		public void SetCompleted ( AbstractTimestamp timestamp )
		{
			if ( !mCancellationToken.IsCancellationRequested )
				mCompletionToken.TrySetResult( timestamp );
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
