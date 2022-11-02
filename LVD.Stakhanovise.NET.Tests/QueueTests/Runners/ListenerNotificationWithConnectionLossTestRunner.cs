using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	internal class ListenerNotificationWithConnectionLossTestRunner : BaseListenerTestRunner, IDisposable
	{
		private readonly PostgreSqlNotificationOperations mNotificationOperations;

		private readonly ManualResetEvent mMaximumReconnectReachedWaitHandle;

		private readonly string mNotificationChannelName;

		private readonly int mReconnectsCount;

		private int mNotificationsReceivedCount = 0;

		private int mReconnectsRemaining = 0;

		public ListenerNotificationWithConnectionLossTestRunner( string listenerDbConnectionString,
			string managementDbConnectionString,
			string notificationChannelName,
			int reconnectsCount )
			: base( managementDbConnectionString )
		{
			mNotificationOperations = new PostgreSqlNotificationOperations( listenerDbConnectionString );
			mMaximumReconnectReachedWaitHandle = new ManualResetEvent( false );
			mNotificationChannelName = notificationChannelName;
			mReconnectsCount = reconnectsCount;
			mReconnectsRemaining = reconnectsCount;
		}

		public override async Task RunTestsAsync( PostgreSqlTaskQueueNotificationListener listener )
		{
			Reset();

			listener.NewTaskPosted += HandleNewTaskPosted;
			listener.ListenerConnectionRestored += HandleListenerConnectionRestored;
			await listener.StartAsync();

			WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
				syncHandle: null,
				delayMilliseconds: RandomDelay() )
					.WithoutAwait();

			mMaximumReconnectReachedWaitHandle
				.WaitOne();

			listener.NewTaskPosted -= HandleNewTaskPosted;
			listener.ListenerConnectionRestored -= HandleListenerConnectionRestored;
			await listener.StopAsync();
		}

		private void HandleListenerConnectionRestored( object sender, ListenerConnectionRestoredEventArgs e )
		{
			mReconnectsRemaining = Math.Max( mReconnectsRemaining - 1, 0 );
			SendChannelNotification();
		}

		private void SendChannelNotification()
		{
			mNotificationOperations.SendChannelNotification( mNotificationChannelName );
		}

		private void HandleNewTaskPosted( object sender, NewTaskPostedEventArgs e )
		{
			PostgreSqlTaskQueueNotificationListener listener =
				( PostgreSqlTaskQueueNotificationListener ) sender;

			mNotificationsReceivedCount++;
			if ( mReconnectsRemaining > 0 )
			{
				WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
					syncHandle: null,
					delayMilliseconds: RandomDelay() );
			}
			else
				mMaximumReconnectReachedWaitHandle.Set();
		}

		private void Reset()
		{
			mNotificationsReceivedCount = 0;
			mReconnectsRemaining = mReconnectsCount;
			mMaximumReconnectReachedWaitHandle.Reset();
		}

		public void Dispose()
		{
			Dispose( true );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				mMaximumReconnectReachedWaitHandle.Dispose();
			}
		}

		public int NotificationsReceivedCount
			=> mNotificationsReceivedCount;

		public int ReconnectsRemaining
			=> mReconnectsRemaining;
	}
}
