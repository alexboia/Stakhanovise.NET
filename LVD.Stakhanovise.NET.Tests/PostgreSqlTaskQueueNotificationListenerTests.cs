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
			Task.Delay( 250 ).Wait();
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanStartAndStopReceivingNotifications_NoConnectionLoss ()
		{
			bool connectedReceived = false;
			bool notificationReceived = false;

			using ( ManualResetEvent notificationReceivedWaitHandle = new ManualResetEvent( false ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.ListenerConnected += ( sender, e ) =>
				{
					connectedReceived = true;
				};

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
				Assert.IsTrue( connectedReceived );

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
			int reconnectsRemaining = 10;

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
							timeout: RandomTimeout() );
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
			int reconnectsRemaining = 10;
			int notificationsReceived = 0;

			using ( ManualResetEvent maximumReconnectReachedWaitHandle = new ManualResetEvent( false ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.NewTaskPosted += ( sender, e ) =>
				{
					notificationsReceived++;
					if ( reconnectsRemaining > 0 )
					{
						WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
							syncHandle: null,
							timeout: RandomTimeout() );
					}
					else
						maximumReconnectReachedWaitHandle.Set();
				};

				listener.ListenerConnectionRestored += async ( sender, e ) =>
				{
					reconnectsRemaining = Math.Max( reconnectsRemaining - 1, 0 );
					await SendChannelNotificationAsync();
				};

				await listener.StartAsync();

				WaitAndTerminateConnection( listener.Diagnostics.ConnectionBackendProcessId,
					syncHandle: null,
					timeout: RandomTimeout() );

				maximumReconnectReachedWaitHandle.WaitOne();
				await listener.StopAsync();

				Assert.AreEqual( 0, reconnectsRemaining );
				Assert.AreEqual( 10, notificationsReceived );
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

		private int RandomTimeout ()
		{
			Random rnd = new Random();
			return rnd.Next( 100, 2000 );
		}

		private string ManagementConnectionString
			=> GetConnectionString( "mgmtDbConnectionString" );

		private string ConnectionString
			=> GetConnectionString( "listenerTestDbConnectionString" );

		private string NotificationChannelname
			=> "sk_test_queue_item_added";
	}
}
