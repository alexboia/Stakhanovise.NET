using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class PostgreSqlQueuedTaskTokenMonitor : IDisposable
	{
		private PostgreSqlQueuedTaskToken mToken;

		private Dictionary<PostgreSqlQueuedTaskTokenConnectionState, Action> mUserCallbacks
			= new Dictionary<PostgreSqlQueuedTaskTokenConnectionState, Action>();

		private ManualResetEvent mTokenConnectionDroppedHandle =
			new ManualResetEvent( false );

		private ManualResetEvent mTokenConnectionEstablishedHandle =
			new ManualResetEvent( false );

		private ManualResetEvent mTokenConnectionFailedHandle =
			new ManualResetEvent( false );

		private ManualResetEvent mTokenConnectionAttemptingToReconnectHandle =
			new ManualResetEvent( false );

		private ManualResetEvent mTokenReleasedEventCalledHandle =
			new ManualResetEvent( false );

		private ManualResetEvent mCancellationTokenInvokedHandle =
			new ManualResetEvent( false );

		private CancellationTokenRegistration mCancellationTokenRegistration;

		public PostgreSqlQueuedTaskTokenMonitor ( PostgreSqlQueuedTaskToken token )
		{
			mToken = token ?? throw new ArgumentNullException( nameof( token ) );

			mToken.TokenReleased += HandleTokenReleased;
			mToken.ConnectionStateChanged += HandleTokenConnectionStateChanged;
			mCancellationTokenRegistration = mToken.CancellationToken.Register( HandleCancellationTokenInvoked );
		}

		public void SetUserCallbackForConnectionStateChange ( PostgreSqlQueuedTaskTokenConnectionState newState, Action userCallback )
		{
			if ( userCallback == null )
				throw new ArgumentNullException( nameof( userCallback ) );

			mUserCallbacks[ newState ] = userCallback;
		}

		private void HandleTokenConnectionStateChanged ( object sender, PostgreSqlQueuedTaskTokenConnectionStateChangeArgs e )
		{
			switch ( e.NewState )
			{
				case PostgreSqlQueuedTaskTokenConnectionState.Dropped:
					mTokenConnectionDroppedHandle.Set();
					break;
				case PostgreSqlQueuedTaskTokenConnectionState.AttemptingToReconnect:
					mTokenConnectionAttemptingToReconnectHandle.Set();
					break;
				case PostgreSqlQueuedTaskTokenConnectionState.Established:
					mTokenConnectionEstablishedHandle.Set();
					break;
				case PostgreSqlQueuedTaskTokenConnectionState.FailedPermanently:
					mTokenConnectionFailedHandle.Set();
					break;
			}

			if ( mUserCallbacks.TryGetValue( e.NewState, out Action userCallback ) )
				userCallback();
		}

		private void HandleTokenReleased ( object sender, TokenReleasedEventArgs e )
		{
			mTokenReleasedEventCalledHandle.Set();
		}

		private void HandleCancellationTokenInvoked ()
		{
			mCancellationTokenInvokedHandle.Set();
		}

		public void Reset ()
		{
			mTokenReleasedEventCalledHandle.Reset();
			mCancellationTokenInvokedHandle.Reset();
			mTokenConnectionDroppedHandle.Reset();
			mTokenConnectionAttemptingToReconnectHandle.Reset();
			mTokenConnectionEstablishedHandle.Reset();
			mTokenConnectionFailedHandle.Reset();
		}

		public void WaitForTokenReleased ()
		{
			mTokenReleasedEventCalledHandle.WaitOne();
		}

		public void WaitForCancellationTokenInvocation ()
		{
			mCancellationTokenInvokedHandle.WaitOne();
		}

		public void WaitForTokenConnectionDroppedInvocation ()
		{
			mTokenConnectionDroppedHandle.WaitOne();
		}

		public void WaitForTokenConnectionAttemptingToReconnectInvocation ()
		{
			mTokenConnectionAttemptingToReconnectHandle.WaitOne();
		}

		public void WaitForConnectionEstablishedInvocation ()
		{
			mTokenConnectionEstablishedHandle.WaitOne();
		}

		public void WaitForConnectionFailedInvocation ()
		{
			mTokenConnectionFailedHandle.WaitOne();
		}

		public void WaitForConnectionEstablishedOrFailedInvocation ()
		{
			WaitHandle.WaitAny( new WaitHandle[]
			{
				mTokenConnectionEstablishedHandle,
				mTokenConnectionFailedHandle
			} );
		}

		protected void Dispose ( bool disposing )
		{
			if ( disposing )
			{
				mTokenReleasedEventCalledHandle.Dispose();
				mCancellationTokenInvokedHandle.Dispose();
				mTokenConnectionDroppedHandle.Dispose();
				mTokenConnectionAttemptingToReconnectHandle.Dispose();
				mTokenConnectionEstablishedHandle.Dispose();
				mTokenConnectionFailedHandle.Dispose();
				mCancellationTokenRegistration.Dispose();

				mToken.TokenReleased -= HandleTokenReleased;
				mToken.ConnectionStateChanged -= HandleTokenConnectionStateChanged;
				mToken = null;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public bool TokenReleasedEventCalled => mTokenReleasedEventCalledHandle
			.WaitOne( TimeSpan.Zero );

		public bool CancellationTokenInvoked => mCancellationTokenInvokedHandle
			.WaitOne( TimeSpan.Zero );

		public bool TokenConnectionDroppedInvoked => mTokenConnectionDroppedHandle
			.WaitOne( TimeSpan.Zero );

		public bool TokenConnectionAttemptingToReconnectInvoked => mTokenConnectionAttemptingToReconnectHandle
			.WaitOne( TimeSpan.Zero );

		public bool TokenConnectionEstablishedInvoked => mTokenConnectionEstablishedHandle
			.WaitOne( TimeSpan.Zero );

		public bool TokenConnectionFailedInvoked => mTokenConnectionFailedHandle
			.WaitOne( TimeSpan.Zero );
	}
}
