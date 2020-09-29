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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: add tests for ComputeAbsoluteTimeTicksAsync
	[TestFixture]
	public class PostgreSqlTaskQueueTimingBeltTests : BaseTestWithConfiguration
	{
		[SetUp]
		public async Task SetUp ()
		{
			await CleanupTimeDbTableAsync();
		}

		[TearDown]
		public async Task TearDown ()
		{
			await CleanupTimeDbTableAsync();
		}

		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 100 )]
		[TestCase( 1, 1000 )]
		[TestCase( 1, 10000 )]

		[TestCase( 2, 0 )]
		[TestCase( 2, 100 )]
		[TestCase( 2, 1000 )]
		[TestCase( 2, 10000 )]

		[TestCase( 5, 0 )]
		[TestCase( 5, 100 )]
		[TestCase( 5, 1000 )]

		[TestCase( 10, 0 )]
		[TestCase( 10, 100 )]
		[TestCase( 10, 1000 )]
		public async Task Test_CanStartStop ( int repeatCycles, int timeBetweenStartStopCalls )
		{
			using ( PostgreSqlTaskQueueTimingBelt tb = GetTimingBelt() )
			{
				for ( int i = 0; i < repeatCycles; i++ )
				{
					await tb.StartAsync();
					Assert.IsTrue( tb.IsRunning );

					if ( timeBetweenStartStopCalls > 0 )
						await Task.Delay( timeBetweenStartStopCalls );

					await tb.StopAsync();
					Assert.IsFalse( tb.IsRunning );
				}
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[NonParallelizable]
		public async Task Test_CanTick_SerialRequests ( int nRequests )
		{
			using ( PostgreSqlTaskQueueTimingBelt tb = GetTimingBelt() )
			{
				await tb.StartAsync();
				Assert.IsTrue( tb.IsRunning );

				long currentWallclockTimeCost = tb.TotalLocalWallclockTimeCost;

				for ( int i = 0; i < nRequests; i++ )
				{
					AbstractTimestamp time = await tb.TickAbstractTimeAsync( 1000 );

					Assert.NotNull( time );
					Assert.AreEqual( i + 1, time.Ticks );
					Assert.AreEqual( currentWallclockTimeCost, time.WallclockTimeCost );
					Assert.AreEqual( ( long )Math.Ceiling( ( double )currentWallclockTimeCost / ( i + 1 ) ), time.TickDuration );

					if ( i < nRequests - 1 )
					{
						long newWallclockTimeCost = ( i + 1 ) * 100;
						tb.AddWallclockTimeCost( newWallclockTimeCost );
						currentWallclockTimeCost += newWallclockTimeCost;
					}
				}

				await tb.StopAsync();
				Assert.IsFalse( tb.IsRunning );
			}
		}

		[Test]
		[TestCase( 2, 1, false )]
		[TestCase( 2, 2, false )]
		[TestCase( 2, 5, false )]
		[TestCase( 2, 10, false )]

		[TestCase( 5, 1, false )]
		[TestCase( 5, 2, false )]
		[TestCase( 5, 5, false )]
		[TestCase( 5, 10, false )]

		[TestCase( 25, 1, false )]
		[TestCase( 25, 2, false )]
		[TestCase( 25, 5, false )]
		[TestCase( 25, 10, false )]

		[TestCase( 2, 1, true )]
		[TestCase( 2, 2, true )]
		[TestCase( 2, 5, true )]
		[TestCase( 2, 10, true )]

		[TestCase( 5, 1, true )]
		[TestCase( 5, 2, true )]
		[TestCase( 5, 5, true )]
		[TestCase( 5, 10, true )]

		[TestCase( 25, 1, true )]
		[TestCase( 25, 2, true )]
		[TestCase( 25, 5, true )]
		[TestCase( 25, 10, true )]
		[NonParallelizable]
		public async Task Test_CanTick_ParallelRequests ( int nThreads, int nRequestsPerThread, bool syncOnCheckPoints )
		{
			Barrier checkpointSync = new Barrier( nThreads );
			List<Task> processingThreads = new List<Task>( nThreads );

			using ( PostgreSqlTaskQueueTimingBelt tb = GetTimingBelt() )
			{
				await tb.StartAsync();
				Assert.IsTrue( tb.IsRunning );

				long currentWallclockTimeCost = tb.TotalLocalWallclockTimeCost;

				for ( int iThread = 0; iThread < nThreads; iThread++ )
				{
					processingThreads.Add( Task.Run( async () =>
					{
						if ( syncOnCheckPoints )
							checkpointSync.SignalAndWait();

						for ( int iRequest = 0; iRequest < nRequestsPerThread; iRequest++ )
						{
							AbstractTimestamp lastThreadTime = tb.LastTime;

							if ( syncOnCheckPoints )
								checkpointSync.SignalAndWait();

							AbstractTimestamp time = await tb.TickAbstractTimeAsync( 10000 );

							Assert.NotNull( time );
							Assert.GreaterOrEqual( time.Ticks,
								lastThreadTime.Ticks );
							Assert.GreaterOrEqual( time.WallclockTimeCost,
								lastThreadTime.WallclockTimeCost );

							if ( iRequest < nRequestsPerThread - 1 )
							{
								long newWallclockTimeCost = iThread + ( iRequest + 1 ) * 100;
								tb.AddWallclockTimeCost( newWallclockTimeCost );
								Interlocked.Add( ref currentWallclockTimeCost, newWallclockTimeCost );
							}
						}
					} ) );
				}

				await Task.WhenAll( processingThreads );

				AbstractTimestamp lastTime = tb.LastTime;

				Assert.NotNull( lastTime );
				Assert.AreEqual( currentWallclockTimeCost,
					tb.TotalLocalWallclockTimeCost );

				Assert.AreEqual( nThreads * nRequestsPerThread,
					lastTime.Ticks );
				Assert.AreEqual( currentWallclockTimeCost,
					lastTime.WallclockTimeCost );

				await tb.StopAsync();
				Assert.IsFalse( tb.IsRunning );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[NonParallelizable]
		public async Task Test_CanAddWallclockTimeCost_SerialCalls ( int nCalls )
		{
			using ( PostgreSqlTaskQueueTimingBelt tb = GetTimingBelt() )
			{
				await tb.StartAsync();
				Assert.IsTrue( tb.IsRunning );

				long currentWallclockTimeCost = tb.TotalLocalWallclockTimeCost;

				for ( int i = 0; i < nCalls; i++ )
				{
					long newWallclockTimeCost = ( i + 1 ) * 100;
					tb.AddWallclockTimeCost( newWallclockTimeCost );
					currentWallclockTimeCost += newWallclockTimeCost;

					Assert.AreEqual( currentWallclockTimeCost, tb.TotalLocalWallclockTimeCost );
					Assert.AreEqual( currentWallclockTimeCost, tb.LocalWallclockTimeCostSinceLastTick );
				}

				await tb.StopAsync();
				Assert.IsFalse( tb.IsRunning );
			}
		}

		[Test]
		[TestCase( 2, 1 )]
		[TestCase( 2, 2 )]
		[TestCase( 2, 5 )]
		[TestCase( 2, 10 )]

		[TestCase( 5, 1 )]
		[TestCase( 5, 2 )]
		[TestCase( 5, 5 )]
		[TestCase( 5, 10 )]

		[TestCase( 25, 1 )]
		[TestCase( 25, 2 )]
		[TestCase( 25, 5 )]
		[TestCase( 25, 10 )]
		[NonParallelizable]
		public async Task Test_CanAddWallclockTimeCost_ParallelCalls ( int nThreads, int nCallsPerThread )
		{
			Barrier syncStart = new Barrier( nThreads );
			List<Task> processingThreads = new List<Task>( nThreads );

			using ( PostgreSqlTaskQueueTimingBelt tb = GetTimingBelt() )
			{
				await tb.StartAsync();
				Assert.IsTrue( tb.IsRunning );

				long currentWallclockTimeCost = tb.TotalLocalWallclockTimeCost;

				for ( int iThread = 0; iThread < nThreads; iThread++ )
				{
					processingThreads.Add( Task.Run( () =>
					{
						syncStart.SignalAndWait();

						for ( int iRequest = 0; iRequest < nCallsPerThread; iRequest++ )
						{
							long newWallclockTimeCost = iThread + ( iRequest + 1 ) * 100;
							tb.AddWallclockTimeCost( newWallclockTimeCost );
							Interlocked.Add( ref currentWallclockTimeCost, newWallclockTimeCost );
						}
					} ) );
				}

				await Task.WhenAll( processingThreads );

				Assert.AreEqual( currentWallclockTimeCost,
					tb.TotalLocalWallclockTimeCost );
				Assert.AreEqual( currentWallclockTimeCost,
					tb.LocalWallclockTimeCostSinceLastTick );

				await tb.StopAsync();
				Assert.IsFalse( tb.IsRunning );
			}
		}

		private async Task CleanupTimeDbTableAsync ()
		{
			string resetSql = @"UPDATE sk_time_t 
				SET t_total_ticks = 0, 
					t_total_ticks_cost = 0 
				WHERE t_id = @t_id";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( resetSql, conn ) )
			{
				cmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid, TimeId );
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private PostgreSqlTaskQueueTimingBelt GetTimingBelt ()
		{
			return new PostgreSqlTaskQueueTimingBelt( GetTimingBeltOptions() );
		}

		private PostgreSqlTaskQueueTimingBeltOptions GetTimingBeltOptions ()
		{
			return new PostgreSqlTaskQueueTimingBeltOptions( TimeId,
				connectionOptions: new ConnectionOptions( ConnectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				initialWallclockTimeCost: 1000,
				timeTickBatchSize: 10,
				timeTickMaxFailCount: 3 );
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync ()
		{
			NpgsqlConnection conn = new NpgsqlConnection( ConnectionString );
			await conn.OpenAsync();
			return conn;
		}

		public string ConnectionString
			=> GetConnectionString( "testDbConnectionString" );

		public Guid TimeId
			=> Guid.Parse( GetAppSetting( "timeId" ) );
	}
}
