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
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
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
		public async Task Test_CanStartStop()
		{
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				await listener.StartAsync();
				Assert.IsTrue( listener.IsStarted );

				await listener.StopAsync();
				Assert.IsFalse( listener.IsStarted );
			}
		}

		[Test]
		[NonParallelizable]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[Repeat( 5 )]
		public async Task Test_CanReceiveNotification_NoConnectionLoss( int notificationSendCount )
		{
			using ( ListenerNotificationWithoutConnectionLossTestRunner runner = CreateListenerNotificationWithoutConnectionLossTestRunner( notificationSendCount ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				await runner.RunTestsAsync( listener );

				Assert.AreEqual( notificationSendCount, runner.NotificationReceivedCount );
				Assert.AreEqual( 1, runner.ConnectedCount );
			}
		}

		private ListenerNotificationWithoutConnectionLossTestRunner CreateListenerNotificationWithoutConnectionLossTestRunner( int notificationSendCount )
		{
			return new ListenerNotificationWithoutConnectionLossTestRunner( ConnectionString,
				ManagementConnectionString,
				NotificationChannelName,
				notificationSendCount );
		}

		[Test]
		[NonParallelizable]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanRecoverFromConnectionLoss( int reconnectsCount )
		{
			using ( ListenerConnectionRecoveryTestRunner runner = CreateListenerConnectionRecoveryTestRunner( reconnectsCount ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				await runner.RunTestsAsync( listener );
				Assert.AreEqual( 0, runner.ReconnectsRemaining );
			}
		}

		private ListenerConnectionRecoveryTestRunner CreateListenerConnectionRecoveryTestRunner( int reconnectsCount )
		{
			return new ListenerConnectionRecoveryTestRunner( ManagementConnectionString,
				reconnectsCount );
		}

		[Test]
		[NonParallelizable]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanReceiveNotifications_WithConnectionLossRecovery( int reconnectsCount )
		{
			using ( ListenerNotificationWithConnectionLossTestRunner runner = CreateListenerNotificationWithConnectionLossTestRunner( reconnectsCount ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				await runner.RunTestsAsync( listener );

				Assert.AreEqual( 0, runner.ReconnectsRemaining );
				Assert.AreEqual( reconnectsCount, runner.NotificationsReceivedCount );
			}
		}

		private ListenerNotificationWithConnectionLossTestRunner CreateListenerNotificationWithConnectionLossTestRunner( int reconnectsCount )
		{
			return new ListenerNotificationWithConnectionLossTestRunner( ConnectionString,
				ManagementConnectionString,
				NotificationChannelName,
				reconnectsCount );
		}

		[Test]
		[NonParallelizable]
		[Repeat( 5 )]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		public async Task Test_DoesNotReceiveNotificationsWhenStopped( int notificationSendCount )
		{
			using ( ListenerNotificationsNotReceivedWhenStoppedTestRunner runner = CreateListenerNotificationsNotReceivedWhenStoppedTestRunner( notificationSendCount ) )
			using ( PostgreSqlTaskQueueNotificationListener listener = CreateListener() )
			{
				await runner.RunTestsAsync(listener );
				Assert.AreEqual( 0, runner.NotificationReceivedCount );
			}
		}

		private ListenerNotificationsNotReceivedWhenStoppedTestRunner CreateListenerNotificationsNotReceivedWhenStoppedTestRunner( int notificationSendCount )
		{
			return new ListenerNotificationsNotReceivedWhenStoppedTestRunner( ConnectionString,
				ManagementConnectionString,
				NotificationChannelName,
				notificationSendCount );
		}

		private PostgreSqlTaskQueueNotificationListener CreateListener()
		{
			TaskQueueListenerOptions options =
				new TaskQueueListenerOptions( ConnectionString,
					NotificationChannelName );

			StandardTaskQueueNotificationListenerMetricsProvider metricsProvider =
				new StandardTaskQueueNotificationListenerMetricsProvider();

			return new PostgreSqlTaskQueueNotificationListener( options,
				metricsProvider,
				CreateLogger() );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}

		private string ConnectionString
			=> GetConnectionString( "listenerTestDbConnectionString" );

		private string NotificationChannelName
			=> "sk_test_queue_item_added";
	}
}
