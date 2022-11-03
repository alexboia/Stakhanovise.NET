// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using System.Timers;
using ServerTimer = System.Timers.Timer;

namespace LVD.Stakhanovise.NET.Model
{
	public class AsyncProcessingRequest<TResult> : IAsyncProcessingRequest<TResult>, IDisposable
	{
		private CancellationToken mCancellationToken;

		private CancellationTokenRegistration mCancellationTokenRegistration;

		private CancellationTokenSource mCancellationTokenSource;

		private TaskCompletionSource<TResult> mCompletionToken;

		private ServerTimer mTimer;

		private bool mIsDisposed;

		private int mCurrentFailCount = 0;

		private int mMaxFailCount;

		private bool mIsTimedOut = false;

		private long mRequestId;

		public AsyncProcessingRequest( long requestId,
			int timeoutMilliseconds,
			int maxFailCount )
		{
			if ( requestId <= 0 )
				throw new ArgumentOutOfRangeException( nameof( requestId ),
					"Request ID must be greater than 0" );

			if ( maxFailCount < 0 )
				throw new ArgumentOutOfRangeException( nameof( maxFailCount ),
					"Maximum allowed fail count must be greater than or equal to 0" );

			mRequestId = requestId;
			mMaxFailCount = maxFailCount;
			mCompletionToken = new TaskCompletionSource<TResult>();

			//If timeout is specified, then schedule the CTS 
			//	to automatically request cancellation
			if ( timeoutMilliseconds > 0 )
				StartCancellationTimer( timeoutMilliseconds );

			SetupCancellation();
		}

		private void SetupCancellation()
		{
			//TODO: simplify cancellation: tokens might not be required after all...
			mCancellationTokenSource =
				new CancellationTokenSource();
			mCancellationToken = mCancellationTokenSource
				.Token;
			mCancellationTokenRegistration = mCancellationToken.Register( ()
				=> HandleCancellationRequested() );
		}

		private void HandleCancellationTimerElapsed( object sender, ElapsedEventArgs e )
		{
			if ( !mIsDisposed && !IsCompleted )
			{
				mIsTimedOut = true;
				mCompletionToken.TrySetException( new TimeoutException( "Request processing timed out" ) );
				mTimer.Elapsed -= HandleCancellationTimerElapsed;
				mTimer.Dispose();
				mTimer = null;
			}
		}

		private void StartCancellationTimer( int timeoutMilliseconds )
		{
			mTimer = new ServerTimer();
			mTimer.Interval = timeoutMilliseconds;
			mTimer.AutoReset = false;
			mTimer.Elapsed += HandleCancellationTimerElapsed;
			mTimer.Start();
		}

		private void HandleCancellationRequested()
		{
			if ( !mIsDisposed )
				mCompletionToken.TrySetCanceled();
		}

		public void SetCancelled()
		{
			if ( !IsCompleted )
			{
				ShutdownCancellationTimer();
				mCancellationTokenSource.Cancel();
			}
		}

		void IAsyncProcessingRequest.SetCompleted( object result )
		{
			SetCompleted( result is TResult ? ( TResult ) result : default( TResult ) );
		}

		public void SetCompleted( TResult result )
		{
			if ( !IsCompleted )
			{
				ShutdownCancellationTimer();
				mCompletionToken.TrySetResult( result );
			}
		}

		public void SetFailed( Exception exc )
		{
			if ( !IsCompleted )
			{
				ShutdownCancellationTimer();
				IncrementFailCount();
				if ( !CanBeRetried )
					mCompletionToken.TrySetException( exc );
			}
		}

		private void ShutdownCancellationTimer()
		{
			if ( mTimer != null )
			{
				mTimer.Elapsed -= HandleCancellationTimerElapsed;
				mTimer.Stop();
				mTimer.Dispose();
				mTimer = null;
			}
		}

		protected virtual void Dispose( bool disposing )
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

		private void IncrementFailCount()
		{
			Interlocked.Increment( ref mCurrentFailCount );
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public Task<TResult> Task
			=> mCompletionToken.Task;

		public bool IsCompleted
			=> mCompletionToken.Task.IsCanceled
				|| mCompletionToken.Task.IsCompleted
				|| mCompletionToken.Task.IsFaulted;

		public bool CanBeRetried
			=> mCurrentFailCount < mMaxFailCount;

		public bool IsTimedOut
			=> mIsTimedOut;

		public long Id
			=> mRequestId;
	}
}
