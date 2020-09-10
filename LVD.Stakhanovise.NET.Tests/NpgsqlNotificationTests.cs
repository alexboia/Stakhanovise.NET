using LVD.Stakhanovise.NET.Tests.Helpers;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class NpgsqlNotificationTests : BaseTestWithConfiguration
	{
		[Test]
		public void Test_ThrowExceptionInNonAwaitedTask ()
		{
			ManualResetEvent exceptionTaskReady =
				new ManualResetEvent( initialState: false );

			Task exceptionTask = Task.Run( () =>
			{
				Task.Delay( 500 ).Wait();
				throw new ApplicationException( "Exception" );
			} );

			exceptionTask.ContinueWith( prev => exceptionTaskReady.Set() );
			exceptionTaskReady.WaitOne();

			Assert.NotNull( exceptionTask.Exception );
			Assert.Throws<AggregateException>( ()
				 => exceptionTask.Wait() );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_TryBreakConnectionWhileListenAndWait ()
		{
			using ( ManualResetEvent syncHandle = new ManualResetEvent( false ) )
			{
				Assert.CatchAsync<NpgsqlException>( async () =>
				{
					CancellationTokenSource cancellation =
						new CancellationTokenSource();

					using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
					{
						await conn.OpenAsync();

						WaitAndTerminateConnection( conn.ProcessID,
							syncHandle: syncHandle,
							timeout: 1000 );

						using ( NpgsqlCommand listenCmd = new NpgsqlCommand( "LISTEN sk_test_break_connection_queue_item_added", conn ) )
							await listenCmd.ExecuteNonQueryAsync();

						syncHandle.Set();
						await conn.WaitAsync( cancellation.Token );
					}
				} );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TryBreakConnectionWhileListenWithoutWait ()
		{
			Task waitTask;
			CancellationTokenSource cancellation =
				new CancellationTokenSource();

			using ( ManualResetEvent syncHandle = new ManualResetEvent( false ) )
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					await conn.OpenAsync();

					WaitAndTerminateConnection( conn.ProcessID,
						syncHandle: syncHandle,
						timeout: 1000 );

					using ( NpgsqlCommand listenCmd = new NpgsqlCommand( "LISTEN sk_test_break_connection_queue_item_added", conn ) )
						await listenCmd.ExecuteNonQueryAsync();

					syncHandle.Set();
					waitTask = conn.WaitAsync( cancellation.Token );

					while ( !waitTask.IsCompleted )
						await Task.Delay( 10 );

					Assert.IsTrue( waitTask.IsCompleted );
					Assert.Throws<AggregateException>( ()
						 => waitTask.Wait() );
				}
			}
		}

		[Test]
		public async Task Test_CollectConnectionStateChanges ()
		{
			List<ConnectionState> connectionStates =
				new List<ConnectionState>();

			using ( ManualResetEvent syncHandle = new ManualResetEvent( false ) )
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					conn.StateChange += ( sender, e ) => connectionStates.Add( e.CurrentState );
					await conn.OpenAsync();

					WaitAndTerminateConnection( conn.ProcessID,
						syncHandle: syncHandle,
						timeout: 1000 );

					using ( NpgsqlCommand queryCmd = new NpgsqlCommand( "select current_timestamp", conn ) )
						await queryCmd.ExecuteNonQueryAsync();

					syncHandle.Set();
					while ( conn.State != ConnectionState.Closed )
						await Task.Delay( 10 );
				}
			}

			Assert.Contains( ConnectionState.Closed,
				connectionStates );
		}

		[Test]
		public async Task Test_ThrowsOperationCancelledOnCancelAfterWaitAsync ()
		{
			bool notificationReceived = false;
			CancellationTokenSource cancellation =
				new CancellationTokenSource();

			using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
			{
				conn.Notification += ( sender, e )
					=> notificationReceived = true;

				await conn.OpenAsync();
				using ( NpgsqlCommand listenCmd = new NpgsqlCommand( "LISTEN sk_test_break_connection_queue_item_added", conn ) )
					await listenCmd.ExecuteNonQueryAsync();

				cancellation.CancelAfter( 1000 );
				Assert.ThrowsAsync<OperationCanceledException>( async ()
					 => await conn.WaitAsync( cancellation.Token ) );
			}

			Assert.IsFalse( notificationReceived );
		}

		[Test]
		public async Task Test_ThrowsOperationCancelledOnCancelWaitAsync ()
		{
			bool notificationReceived = false;
			CancellationTokenSource cancellation =
				new CancellationTokenSource();

			using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
			{
				conn.Notification += ( sender, e )
					=> notificationReceived = true;

				await conn.OpenAsync();
				using ( NpgsqlCommand listenCmd = new NpgsqlCommand( "LISTEN sk_test_break_connection_queue_item_added", conn ) )
					await listenCmd.ExecuteNonQueryAsync();

				Task.Delay( 1000 )
					.ContinueWith( prev => cancellation.Cancel() )
					.WithoutAwait();

				Assert.ThrowsAsync<OperationCanceledException>( async ()
					 => await conn.WaitAsync( cancellation.Token ) );
			}

			Assert.IsFalse( notificationReceived );
		}

		[Test]
		public async Task Test_CanListenForNotifications ()
		{
			Task tListen, tNotify;
			List<NpgsqlNotificationEventArgs> notificationData = new List<NpgsqlNotificationEventArgs>();

			tListen = Task.Run( async () =>
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					await conn.OpenAsync();
					conn.Notification += ( sender, e ) => notificationData.Add( e );

					using ( NpgsqlCommand listenCmd = new NpgsqlCommand( "LISTEN sk_test_queue_item_added", conn ) )
						await listenCmd.ExecuteNonQueryAsync();

					while ( notificationData.Count < 5 )
						await conn.WaitAsync();

					using ( NpgsqlCommand unlistenCmd = new NpgsqlCommand( "UNLISTEN sk_test_queue_item_added", conn ) )
						await unlistenCmd.ExecuteNonQueryAsync();

					conn.Close();
				}
			} );

			await Task.Delay( 250 );

			tNotify = Task.Run( async () =>
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					await conn.OpenAsync();

					for ( int i = 1; i <= 5; i++ )
					{
						using ( NpgsqlCommand listenCmd = new NpgsqlCommand( $"NOTIFY sk_test_queue_item_added, '{i.ToString()}'", conn ) )
							await listenCmd.ExecuteNonQueryAsync();

						await Task.Delay( 100 );
					}

					conn.Close();
				}
			} );

			await tNotify;
			await tListen;

			Assert.AreEqual( 5, notificationData.Count );

			foreach ( NpgsqlNotificationEventArgs noteData in notificationData )
				Assert.AreEqual( "sk_test_queue_item_added", noteData.Channel );

			Assert.NotNull( notificationData.Any( n => n.Payload.Equals( "1" ) ) );
			Assert.NotNull( notificationData.Any( n => n.Payload.Equals( "2" ) ) );
			Assert.NotNull( notificationData.Any( n => n.Payload.Equals( "3" ) ) );
			Assert.NotNull( notificationData.Any( n => n.Payload.Equals( "4" ) ) );
			Assert.NotNull( notificationData.Any( n => n.Payload.Equals( "5" ) ) );
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
			=> GetConnectionString( "testDbConnectionString" );
	}
}
