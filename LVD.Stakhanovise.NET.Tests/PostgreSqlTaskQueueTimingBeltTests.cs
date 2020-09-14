using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueTimingBeltTests : BaseTestWithConfiguration
	{
		[SetUp]
		public async Task SetUp ()
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
					Assert.AreEqual( i + 1, time.CurrentTicks );
					Assert.AreEqual( currentWallclockTimeCost, time.CurrentTicksWallclockTimeCost );
					Assert.AreEqual( ( i + 1 ) / currentWallclockTimeCost, time.TickDuration );

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
		public async Task Test_CanTick_ParallelRequests ( int nThreads, int nRequestsPerThread )
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
					processingThreads.Add( Task.Run( async () =>
					{
						syncStart.SignalAndWait();

						for ( int iRequest = 0; iRequest < nRequestsPerThread; iRequest++ )
						{
							AbstractTimestamp lastThreadTime = tb.LastTime;
							AbstractTimestamp time = await tb.TickAbstractTimeAsync( 10000 );

							Assert.NotNull( time );
							Assert.GreaterOrEqual( time.CurrentTicks,
								lastThreadTime.CurrentTicks );
							Assert.GreaterOrEqual( time.CurrentTicksWallclockTimeCost,
								lastThreadTime.CurrentTicksWallclockTimeCost );

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
					lastTime.CurrentTicks );
				Assert.AreEqual( currentWallclockTimeCost,
					lastTime.CurrentTicksWallclockTimeCost );

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

		private PostgreSqlTaskQueueTimingBelt GetTimingBelt ()
		{
			return new PostgreSqlTaskQueueTimingBelt( TimeId, timeConnectionString: ConnectionString,
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

		public string ConnectionString => GetConnectionString( "testDbConnectionString" );

		public Guid TimeId => Guid.Parse( "9eca068e-c4e2-49db-b537-50d74381a4bb" );
	}
}
