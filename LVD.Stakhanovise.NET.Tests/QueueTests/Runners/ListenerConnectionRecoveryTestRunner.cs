using LVD.Stakhanovise.NET.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	public class ListenerConnectionRecoveryTestRunner : BaseListenerTestRunner, IDisposable
	{
		private readonly ManualResetEvent mMaxReconnectsReachedWaitHandle;

		private readonly int mReconnectsCount;

		private int mReconnectsRemaining;

		public ListenerConnectionRecoveryTestRunner( string managementDbConnectionString,
			int reconnectsCount )
			: base( managementDbConnectionString )
		{
			mMaxReconnectsReachedWaitHandle = new ManualResetEvent( false );
			mReconnectsCount = reconnectsCount;
		}

		public override async Task RunTestsAsync( PostgreSqlTaskQueueNotificationListener listener )
		{
			Reset();

			listener.ListenerConnectionRestored += HandleListenerConnectionRestored;
			await listener.StartAsync();

			WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
				syncHandle: null,
				delayMilliseconds: RandomDelay() )
					.WithoutAwait();

			mMaxReconnectsReachedWaitHandle
				.WaitOne();

			listener.ListenerConnectionRestored -= HandleListenerConnectionRestored;
			await listener.StopAsync();
		}

		private void HandleListenerConnectionRestored( object sender, ListenerConnectionRestoredEventArgs e )
		{
			PostgreSqlTaskQueueNotificationListener listener =
				( PostgreSqlTaskQueueNotificationListener ) sender;

			mReconnectsRemaining = Math
				.Max( mReconnectsRemaining - 1, 0 );

			if ( mReconnectsRemaining > 0 )
			{
				WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
					syncHandle: null,
					delayMilliseconds: RandomDelay() );
			}
			else
				mMaxReconnectsReachedWaitHandle.Set();
		}

		private void Reset()
		{
			mReconnectsRemaining = mReconnectsCount;
			mMaxReconnectsReachedWaitHandle.Reset();
		}

		public void Dispose()
		{
			Dispose( true );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				mMaxReconnectsReachedWaitHandle.Dispose();
			}
		}

		public int ReconnectsRemaining
			=> mReconnectsRemaining;
	}
}
