using LVD.Stakhanovise.NET.Queue;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Tests.Helpers;
using Npgsql.Logging;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class PostgreSqlTaskQueueNotificationListenerTests : BaseTestWithConfiguration
	{
		[TearDown]
		public void TestTearDown ()
		{
			Task.Delay( 1000 ).Wait();
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
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
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanRecoverFromConnectionLoss ()
		{
			int reconnectsRemaining = 3;

			using ( ManualResetEvent maxReconnectsReachedWaitHandle = new ManualResetEvent( false ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.ListenerConnectionRestored += ( sender, e ) =>
				{
					reconnectsRemaining = Math.Max( reconnectsRemaining - 1, 0 );
					if ( reconnectsRemaining > 0 )
					{
						WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
							syncHandle: null,
							timeout: 1000 );
					}
					else
						maxReconnectsReachedWaitHandle.Set();
				};

				await listener.StartAsync();

				WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
					syncHandle: null,
					timeout: 1000 );

				maxReconnectsReachedWaitHandle.WaitOne();
				Assert.AreEqual( 0, reconnectsRemaining );

				await listener.StopAsync();
			}

		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanStartAndStopReceivingNotifications_WithConnectionLossRecovery ()
		{
			int reconnectsRemaining = 3;
			int notificationsReceived = 0;

			using ( ManualResetEvent maximumReconnectReachedWaitHandle = new ManualResetEvent( false ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.NewTaskPosted += ( sender, e ) =>
				{
					notificationsReceived++;
					Console.WriteLine( $"New task received. Reconnects remaining={reconnectsRemaining}" );
					if ( reconnectsRemaining > 0 )
					{
						WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
							syncHandle: null,
							timeout: 1000 );
					}
					else
						maximumReconnectReachedWaitHandle.Set();
				};

				listener.ListenerConnectionRestored += async ( sender, e ) =>
				{
					reconnectsRemaining = Math.Max( reconnectsRemaining - 1, 0 );
					await SendChannelNotificationAsync();
					Console.WriteLine( "Connection restored. Notification sent." );
				};

				await listener.StartAsync();

				WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
					syncHandle: null,
					timeout: 1000 );

				maximumReconnectReachedWaitHandle.WaitOne();
				Console.WriteLine( "Will stop listening for tasks" );
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
				await db.CloseAsync();
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

		private void WaitAndTerminateConnection ( int pid, ManualResetEvent syncHandle, int timeout )
		{
			Task.Run( async () =>
			{
				using ( NpgsqlConnection mgmtConn = new NpgsqlConnection( ManagementConnectionString ) )
				{
					await mgmtConn.WaitAndTerminateConnectionAsync( pid,
						syncHandle,
						timeout );
				}
			} );
		}

		private string ManagementConnectionString
			=> GetConnectionString( "mgmtDbConnectionString" );

		private string ConnectionString 
			=> GetConnectionString( "listenerTestDbConnectionString" );

		private string NotificationChannelname =>
			"sk_test_queue_item_added";
	}
}
