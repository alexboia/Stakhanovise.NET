﻿// 
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
using Bogus;
using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class NpgsqlQueryExtensionsTests : BaseDbTests
	{
		[Test]
		[TestCase( "sk_sample_channel_name", true )]
		[TestCase( "sk_sample_channel_name", false )]
		[Repeat( 5 )]
		public async Task Test_CanListenUnlisten ( string channelName, bool sendSampleNotification )
		{
			using ( ManualResetEvent notificationReceived = new ManualResetEvent( false ) )
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			{
				NotificationEventHandler neh = ( s, e )
					=> notificationReceived.Set();

				Assert.IsFalse( conn.IsListening( channelName ) );

				await conn.ListenAsync( channelName, neh );
				Assert.IsTrue( conn.IsListening( channelName ) );

				if ( sendSampleNotification )
				{
					using ( NpgsqlConnection connSend = await OpenDbConnectionAsync( ConnectionString ) )
					{
						await connSend.NotifyAsync( channelName,
							withinTx: null );
						await connSend.CloseAsync();
					}

					conn.Wait();
					notificationReceived.WaitOne();
				}
				else
					await Task.Delay( 500 );

				await conn.UnlistenAsync( channelName, neh );
				Assert.IsFalse( conn.IsListening( channelName ) );

				await conn.CloseAsync();
			}
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanLockUnlock ()
		{
			Faker faker =
				new Faker();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			{
				long lock1 = faker.Random.Long( 1, 10000 );
				long lock2 = faker.Random.Long( 1, 10000 );

				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock2 ) );

				await conn.LockAsync( lock1 );
				Assert.IsTrue( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock2 ) );

				await conn.LockAsync( lock2 );
				Assert.IsTrue( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsTrue( await conn.IsAdvisoryLockHeldAsync( lock2 ) );

				await conn.UnlockAsync( lock1 );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsTrue( await conn.IsAdvisoryLockHeldAsync( lock2 ) );

				await conn.UnlockAsync( lock2 );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock2 ) );
			}
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanLockUnlock_WithUnlockAll ()
		{
			Faker faker =
				new Faker();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			{
				long lock1 = faker.Random.Long( 1, 10000 );
				long lock2 = faker.Random.Long( 1, 10000 );

				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock2 ) );

				await conn.LockAsync( lock1 );
				await conn.LockAsync( lock2 );

				await conn.UnlockAllAsync();
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock1 ) );
				Assert.IsFalse( await conn.IsAdvisoryLockHeldAsync( lock2 ) );
			}
		}

		private string ConnectionString
			=> GetConnectionString( "testDbConnectionString" );
	}
}
