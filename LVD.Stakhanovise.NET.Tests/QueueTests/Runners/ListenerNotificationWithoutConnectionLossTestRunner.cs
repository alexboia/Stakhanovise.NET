using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	public class ListenerNotificationWithoutConnectionLossTestRunner : BaseListenerTestRunner, IDisposable
	{
		private readonly DbNotificationOperations mNotificationOperations;

		private readonly ManualResetEvent mNotificationReceivedWaitHandle;

		private readonly string mNotificationChannelName;

		private readonly int mNotificationSendCount;

		private int mConnectedCount = 0;

		private int mNotificationReceivedCount = 0;

		public ListenerNotificationWithoutConnectionLossTestRunner( string listenerDbConnectionString,
			string managementDbConnectionString,
			string notificationChannelName,
			int notificationSendCount )
			: base( managementDbConnectionString )
		{
			mNotificationOperations = new DbNotificationOperations( listenerDbConnectionString );
			mNotificationReceivedWaitHandle = new ManualResetEvent( false );
			mNotificationChannelName = notificationChannelName;
			mNotificationSendCount = notificationSendCount;
		}

		public override async Task RunTestsAsync( PostgreSqlTaskQueueNotificationListener listener )
		{
			Reset();

			listener.ListenerConnected += HandleListenerConnected;
			listener.NewTaskPosted += HandleNewTaskPosted;
			await listener.StartAsync();

			await SendChannelNotificationAsync();
			mNotificationReceivedWaitHandle.WaitOne();

			listener.ListenerConnected -= HandleListenerConnected;
			listener.NewTaskPosted -= HandleNewTaskPosted;
			await listener.StopAsync();
		}

		private async void HandleNewTaskPosted( object sender, NewTaskPostedEventArgs e )
		{
			mNotificationReceivedCount += 1;
			if ( mNotificationReceivedCount < mNotificationSendCount )
				await SendChannelNotificationAsync();
			else
				mNotificationReceivedWaitHandle.Set();
		}

		private void HandleListenerConnected( object sender, ListenerConnectedEventArgs e )
		{
			mConnectedCount += 1;
		}

		private async Task SendChannelNotificationAsync()
		{
			await mNotificationOperations.SendChannelNotificationAsync( mNotificationChannelName );
		}

		private void Reset()
		{
			mConnectedCount = 0;
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

		public int ConnectedCount
			=> mConnectedCount;

		public int NotificationReceivedCount
			=> mNotificationReceivedCount;
	}
}
