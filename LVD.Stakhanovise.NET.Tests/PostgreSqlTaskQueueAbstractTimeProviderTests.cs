using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueAbstractTimeProviderTests : BaseTestWithConfiguration
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
		[TestCase( 0, 0 )]
		[TestCase( 10, 100 )]
		[TestCase( 250, 3000 )]
		public async Task Test_CanComputeAbsoluteTimeTicksAsync ( long currentTicks, long currentTicksCost )
		{
			await SetTimeDbTableValuesAsync( currentTicks, currentTicksCost );

			PostgreSqlTaskQueueAbstractTimeProvider timeProvider = 
				GetTimeProvider();

			long abstractTimeTicks = await timeProvider
				.ComputeAbsoluteTimeTicksAsync( 100 );

			Assert.AreEqual( currentTicks + 100, 
				abstractTimeTicks );
		}

		[Test]
		[TestCase( 0, 0 )]
		[TestCase( 10, 100 )]
		[TestCase( 250, 3000 )]
		public async Task Test_GetCurrentTimeAsync ( long currentTicks, long currentTicksCost )
		{
			await SetTimeDbTableValuesAsync( currentTicks, currentTicksCost );

			PostgreSqlTaskQueueAbstractTimeProvider timeProvider =
				GetTimeProvider();

			AbstractTimestamp now = await timeProvider
				.GetCurrentTimeAsync();

			Assert.NotNull( now );
			Assert.AreEqual( currentTicks, now.Ticks );
			Assert.AreEqual( currentTicksCost, now.WallclockTimeCost );
		}

		private async Task SetTimeDbTableValuesAsync ( long ticks, long totalCost )
		{
			string resetSql = @"UPDATE sk_time_t 
				SET t_total_ticks = @t_total_ticks, 
					t_total_ticks_cost = @t_total_ticks_cost 
				WHERE t_id = @t_id";

			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( resetSql, conn ) )
			{
				cmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
					TimeId );
				cmd.Parameters.AddWithValue( "t_total_ticks", NpgsqlDbType.Bigint,
					ticks );
				cmd.Parameters.AddWithValue( "t_total_ticks_cost", NpgsqlDbType.Bigint,
					totalCost );

				await cmd.PrepareAsync();
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
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
				cmd.Parameters.AddWithValue( "t_id", NpgsqlDbType.Uuid,
					TimeId );

				await cmd.PrepareAsync();
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private PostgreSqlTaskQueueAbstractTimeProvider GetTimeProvider ()
		{
			return new PostgreSqlTaskQueueAbstractTimeProvider( new PostgreSqlTaskQueueAbstractTimeProviderOptions( TimeId, 
				new ConnectionOptions( ConnectionString, 
					keepAliveSeconds: 0, 
					retryCount: 3, 
					retryDelayMilliseconds: 100 ) ) );
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
