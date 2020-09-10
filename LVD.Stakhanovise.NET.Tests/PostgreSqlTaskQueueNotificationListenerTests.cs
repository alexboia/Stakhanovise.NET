using LVD.Stakhanovise.NET.Queue;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueNotificationListenerTests : BaseTestWithConfiguration
	{
		[Test]
		public async Task Test_CanStartAndStopReceivingNotifications_NoConnectionLoss ()
		{
			bool notificationReceived = false;
			ManualResetEvent notificationReceivedWaitHandle =
				new ManualResetEvent( initialState: false );

			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.NewTaskPosted += ( sender, e ) =>
				{
					notificationReceived = true;
					notificationReceivedWaitHandle.Set();
				};

				await listener.StartAsync();
				Assert.IsTrue( listener.IsStarted );

				await SendChannelNotificationAsync();
				notificationReceivedWaitHandle.WaitOne();

				Assert.IsTrue( notificationReceived );

				notificationReceived = false;
				notificationReceivedWaitHandle.Reset();

				await listener.StopAsync();
				Assert.IsFalse( listener.IsStarted );

				await SendChannelNotificationAsync();
				bool signalReceived = notificationReceivedWaitHandle.WaitOne( 1000 );

				Assert.IsFalse( notificationReceived );
				Assert.IsFalse( signalReceived );
			}
		}

		[Test]
		public async Task Test_CanRecoverFromConnectionLoss ()
		{
			int reconnectsRemaining = 3;
			ManualResetEvent maxReconnectsReachedWaitHandle =
				new ManualResetEvent( initialState: false );

			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.ListenerConnectionRestored += ( sender, e ) =>
				{
					reconnectsRemaining = Math.Max( reconnectsRemaining - 1, 0 );
					if ( reconnectsRemaining <= 0 )
						maxReconnectsReachedWaitHandle.Set();
				};

				await listener.StartAsync();

				maxReconnectsReachedWaitHandle.WaitOne();
				Assert.AreEqual( 0, reconnectsRemaining );

				await listener.StopAsync();
			}
		}

		[Test]
		public async Task Test_CanStartAndStopReceivingNotifications_WithConnectionLossRecovery ()
		{
			int reconnectsRemaining = 3;
			int notificationsReceived = 0;
			ManualResetEvent maximumReconnectReachedWaitHandle =
				new ManualResetEvent( initialState: false );

			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.NewTaskPosted += ( sender, e ) =>
				{
					notificationsReceived++;
					if ( reconnectsRemaining == 0 )
						maximumReconnectReachedWaitHandle.Set();
				};

				listener.ListenerConnectionRestored += async ( sender, e ) =>
				{
					reconnectsRemaining = Math.Max( reconnectsRemaining - 1, 0 );
					await SendChannelNotificationAsync();
				};

				await listener.StartAsync();
				maximumReconnectReachedWaitHandle.WaitOne();
				await listener.StopAsync();

				Assert.AreEqual( 0, reconnectsRemaining );
				Assert.AreEqual( 3, notificationsReceived );
			}
		}

		private async Task SendChannelNotificationAsync ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				await db.NotifyAsync( NotificationChannelname, null );
				db.Close();
			}

			await Task.Delay( 100 );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( ConnectionString );
			await db.OpenAsync();
			return db;
		}

		private PostgreSqlTaskQueueNotificationListener CreateListener ()
		{
			return new PostgreSqlTaskQueueNotificationListener( ConnectionString,
				NotificationChannelname );
		}

		private string ConnectionString => GetConnectionString( "listenerTestDbConnectionString" );

		private string NotificationChannelname => "sk_test_queue_item_added";
	}
}
