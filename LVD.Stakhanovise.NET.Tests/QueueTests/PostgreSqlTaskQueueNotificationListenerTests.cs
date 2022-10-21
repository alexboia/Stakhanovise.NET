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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using Npgsql;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	[TestFixture]
	[NonParallelizable]
	public class PostgreSqlTaskQueueNotificationListenerTests : BaseDbTests
	{
		[TearDown]
		public void TestTearDown()
		{
			Task.Delay( 250 ).Wait();
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanStartAndStopReceivingNotifications_NoConnectionLoss()
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

				//Start and wait for a test notification
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

				//Send the notification again, it should not be received
				await SendChannelNotificationAsync();
				bool signalReceived = notificationReceivedWaitHandle.WaitOne( 1000 );

				Assert.IsFalse( notificationReceived );
				Assert.IsFalse( signalReceived );
			}
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		public async Task Test_CanRecoverFromConnectionLoss()
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
						WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
							syncHandle: null,
							timeout: RandomTimeout() );
					}
					else
						maxReconnectsReachedWaitHandle.Set();
				};

				await listener.StartAsync();

				WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
					syncHandle: null,
					timeout: 1000 ).WithoutAwait();

				maxReconnectsReachedWaitHandle.WaitOne();
				Assert.AreEqual( 0, reconnectsRemaining );

				await listener.StopAsync();
			}
		}

		[Test]
		[NonParallelizable]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanStartAndStopReceivingNotifications_WithConnectionLossRecovery( int reconnectsCount )
		{
			int notificationsReceived = 0;
			int reconnectsRemaining = reconnectsCount;

			using ( ManualResetEvent maximumReconnectReachedWaitHandle = new ManualResetEvent( false ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				listener.NewTaskPosted += ( sender, e ) =>
				{
					notificationsReceived++;
					if ( reconnectsRemaining > 0 )
					{
						WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
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

				WaitAndTerminateConnectionAsync( listener.ListenerConnectionBackendProcessId,
					syncHandle: null,
					timeout: RandomTimeout() ).WithoutAwait();

				maximumReconnectReachedWaitHandle.WaitOne();
				await listener.StopAsync();

				Assert.AreEqual( 0, reconnectsRemaining );
				Assert.AreEqual( reconnectsCount, notificationsReceived );
			}
		}

		private async Task SendChannelNotificationAsync()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				await db.NotifyAsync( NotificationChannelname, null );
				await db.CloseAsync();
			}

			await Task.Delay( 100 );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync()
		{
			return await OpenDbConnectionAsync( ConnectionString );
		}

		private PostgreSqlTaskQueueNotificationListener CreateListener()
		{
			TaskQueueListenerOptions options =
				new TaskQueueListenerOptions( ConnectionString,
					NotificationChannelname );

			StandardTaskQueueNotificationListenerMetricsProvider metricsProvider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			return new PostgreSqlTaskQueueNotificationListener( options,
				metricsProvider,
				CreateLogger() );
		}

		private int RandomTimeout()
		{
			Random rnd = new Random();
			return rnd.Next( 100, 2000 );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}

		private string ConnectionString
			=> GetConnectionString( "listenerTestDbConnectionString" );

		private string NotificationChannelname
			=> "sk_test_queue_item_added";
	}
}
