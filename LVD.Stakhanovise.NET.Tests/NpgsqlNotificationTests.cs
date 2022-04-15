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
	public class NpgsqlNotificationTests : BaseDbTests
	{
		[Test]
		public void Test_ThrowExceptionInNonAwaitedTask()
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
		public void Test_TryBreakConnectionWhileListenAndWait()
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

						WaitAndTerminateConnectionAsync( conn.ProcessID,
							syncHandle: syncHandle,
							timeout: 1000 ).WithoutAwait();

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
		public async Task Test_TryBreakConnectionWhileListenWithoutWait()
		{
			Task waitTask;
			CancellationTokenSource cancellation =
				new CancellationTokenSource();

			using ( ManualResetEvent syncHandle = new ManualResetEvent( false ) )
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					await conn.OpenAsync();

					WaitAndTerminateConnectionAsync( conn.ProcessID,
						syncHandle: syncHandle,
						timeout: 1000 ).WithoutAwait();

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
		public async Task Test_CollectConnectionStateChanges()
		{
			List<ConnectionState> connectionStates =
				new List<ConnectionState>();

			using ( ManualResetEvent syncHandle = new ManualResetEvent( false ) )
			{
				using ( NpgsqlConnection conn = new NpgsqlConnection( ConnectionString ) )
				{
					conn.StateChange += ( sender, e ) => connectionStates.Add( e.CurrentState );
					await conn.OpenAsync();

					WaitAndTerminateConnectionAsync( conn.ProcessID,
						syncHandle: syncHandle,
						timeout: 1000 ).WithoutAwait();

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
		public async Task Test_ThrowsOperationCancelledOnCancelAfterWaitAsync()
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
				Assert.ThrowsAsync( Is.InstanceOf<OperationCanceledException>(), async ()
					 => await conn.WaitAsync( cancellation.Token ) );
			}

			Assert.IsFalse( notificationReceived );
		}

		[Test]
		public async Task Test_ThrowsOperationCancelledOnCancelWaitAsync()
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

				Assert.ThrowsAsync( Is.InstanceOf<OperationCanceledException>(), async ()
					 => await conn.WaitAsync( cancellation.Token ) );
			}

			Assert.IsFalse( notificationReceived );
		}

		[Test]
		public async Task Test_CanListenForNotifications()
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

		private string ConnectionString
			=> GetConnectionString( "listenerTestDbConnectionString" );
	}
}
