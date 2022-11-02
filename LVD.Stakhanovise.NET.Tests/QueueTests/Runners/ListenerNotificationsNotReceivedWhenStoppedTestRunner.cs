using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	public class ListenerNotificationsNotReceivedWhenStoppedTestRunner : BaseListenerTestRunner, IDisposable
	{
		private readonly PostgreSqlNotificationOperations mNotificationOperations;

		private readonly ManualResetEvent mNotificationReceivedWaitHandle;

		private readonly string mNotificationChannelName;

		private readonly int mNotificationSendCount;

		private int mNotificationReceivedCount = 0;

		public ListenerNotificationsNotReceivedWhenStoppedTestRunner(
			string listenerDbConnectionString,
			string managementDbConnectionString,
			string notificationChannelName,
			int notificationSendCount ) 
			: base( managementDbConnectionString )
		{
			mNotificationOperations = new PostgreSqlNotificationOperations( listenerDbConnectionString );
			mNotificationReceivedWaitHandle = new ManualResetEvent( false );
			mNotificationChannelName = notificationChannelName;
			mNotificationSendCount = notificationSendCount;
		}

		public override async Task RunTestsAsync( PostgreSqlTaskQueueNotificationListener listener )
		{
			Reset();
			listener.NewTaskPosted += HandleNewTaskPosted;

			for ( int i = 0; i < mNotificationSendCount; i++ )
				await SendChannelNotificationAsync();

			mNotificationReceivedWaitHandle
				.WaitOne( 1500 );
		}

		private void HandleNewTaskPosted( object sender, NewTaskPostedEventArgs e )
		{
			mNotificationReceivedCount += 1;
			mNotificationReceivedWaitHandle.Set();
		}

		private async Task SendChannelNotificationAsync()
		{
			await mNotificationOperations.SendChannelNotificationAsync( mNotificationChannelName );
		}

		private void Reset()
		{
			mNotificationReceivedCount = 0;
			mNotificationReceivedWaitHandle.Reset();
		}

		public void Dispose()
		{
			Dispose( true );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				mNotificationReceivedWaitHandle.Dispose();
			}
		}

		public int NotificationReceivedCount
			=> mNotificationReceivedCount;
	}
}
