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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlExecutionPerformanceMonitorWriterTests : BaseDbTests
	{
		[SetUp]
		public async Task SetUp ()
		{
			await CleanupPerformanceMonitorTableAsync();
		}

		[TearDown]
		public async Task TearDown ()
		{
			await CleanupPerformanceMonitorTableAsync();
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public async Task Test_CanWrite_NoExistingStats_UniquePayloadTypes ( int nStats )
		{
			Faker faker =
				new Faker();

			PostgreSqlExecutionPerformanceMonitorWriter writer =
				GetWriter();

			List<TaskPerformanceStats> sampleStats = DuplicatePayloadTypes( faker
				.RandomExecutionPerformanceStats( nStats ) );

			List<ExecutionPerformanceInfoRecord> expectedRecords =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( sampleStats );

			await writer.WriteAsync( sampleStats );

			List<ExecutionPerformanceInfoRecord> dbRecords =
				await GetDbTaskPerformanceStatsAsync();

			CollectionAssert.AreEquivalent( expectedRecords,
				dbRecords );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public async Task Test_CanWrite_NoExistingStats_WithDuplicatePayloadTypes ( int nStats )
		{
			Faker faker =
				new Faker();

			PostgreSqlExecutionPerformanceMonitorWriter writer =
				GetWriter();

			List<TaskPerformanceStats> sampleStats = faker
				.RandomExecutionPerformanceStats( nStats );

			List<ExecutionPerformanceInfoRecord> expectedRecords =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( sampleStats );

			await writer.WriteAsync( sampleStats );

			List<ExecutionPerformanceInfoRecord> dbRecords =
				await GetDbTaskPerformanceStatsAsync();

			CollectionAssert.AreEquivalent( expectedRecords,
				dbRecords );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public async Task Test_CanWrite_WithExistingStats_UniquePayloadTypesInSet_SamePayloadTypesAsExisting ( int nStats )
		{
			Faker faker =
				new Faker();

			PostgreSqlExecutionPerformanceMonitorWriter writer =
				GetWriter();

			List<TaskPerformanceStats> dbStats =
				await GenerateExecutionPerformanceStatsInDbAsync( nStats );

			List<TaskPerformanceStats> newStats = faker.RandomExecutionPerformanceStats( dbStats
				.Select( s => s.PayloadType )
				.AsEnumerable() );

			List<ExecutionPerformanceInfoRecord> expectedRecords =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( dbStats, newStats );

			await writer.WriteAsync( newStats );

			List<ExecutionPerformanceInfoRecord> dbRecords =
				await GetDbTaskPerformanceStatsAsync();

			CollectionAssert.AreEquivalent( expectedRecords,
				dbRecords );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public async Task Test_CanWrite_WithExistingStats_UniquePayloadTypesInSet_DifferentPayloadTypesThanExisting ( int nStats )
		{
			Faker faker =
				new Faker();

			PostgreSqlExecutionPerformanceMonitorWriter writer =
				GetWriter();

			List<TaskPerformanceStats> dbStats =
				await GenerateExecutionPerformanceStatsInDbAsync( nStats );

			List<TaskPerformanceStats> newStats =
				faker.RandomExecutionPerformanceStats( nStats );

			List<ExecutionPerformanceInfoRecord> expectedRecords =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( dbStats, newStats );

			await writer.WriteAsync( newStats );

			List<ExecutionPerformanceInfoRecord> dbRecords =
				await GetDbTaskPerformanceStatsAsync();

			CollectionAssert.AreEquivalent( expectedRecords,
				dbRecords );
		}

		[Test]
		[TestCase( 1, 1 )]
		[TestCase( 2, 1 )]
		[TestCase( 5, 1 )]
		[TestCase( 10, 1 )]
		[TestCase( 100, 1 )]

		[TestCase( 1, 2 )]
		[TestCase( 2, 2 )]
		[TestCase( 5, 2 )]
		[TestCase( 10, 2 )]
		[TestCase( 100, 2 )]

		[TestCase( 1, 5 )]
		[TestCase( 2, 5 )]
		[TestCase( 5, 5 )]
		[TestCase( 10, 5 )]
		[TestCase( 100, 5 )]
		[Repeat( 10 )]
		public async Task Test_CanWrite_AllZeroValues_NoInitialValues_SamePayloadTypes ( int nStats, int nWrites )
		{
			Faker faker =
				new Faker();

			PostgreSqlExecutionPerformanceMonitorWriter writer =
				GetWriter();

			List<TaskPerformanceStats>[] sampleStats =
				new List<TaskPerformanceStats>[ nWrites ];

			for ( int i = 0; i < nWrites; i++ )
			{
				if ( i > 0 )
				{
					sampleStats[ i ] = faker.RandomAllZeroExecutionPerformanceStats( sampleStats[ 0 ]
						.Select( s => s.PayloadType )
						.AsEnumerable() );
				}	
				else
					sampleStats[ i ] = faker.RandomAllZeroExecutionPerformanceStats( nStats );
			}

			List<ExecutionPerformanceInfoRecord> expectedRecords =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( sampleStats );

			for ( int i = 0; i < nWrites; i++ )
				await writer.WriteAsync( sampleStats[ i ] );

			List<ExecutionPerformanceInfoRecord> dbRecords =
				await GetDbTaskPerformanceStatsAsync();

			CollectionAssert.AreEquivalent( expectedRecords,
				dbRecords );

			foreach ( ExecutionPerformanceInfoRecord r in dbRecords )
			{
				Assert.AreEqual( nWrites, r.NExecutionCycles );
				Assert.IsTrue( r.AllZeroValues() );
			}
		}

		private async Task<List<TaskPerformanceStats>> GenerateExecutionPerformanceStatsInDbAsync ( int nStats )
		{
			Faker faker =
				new Faker();

			List<TaskPerformanceStats> dbStats = faker
				.RandomExecutionPerformanceStats( nStats );

			List<ExecutionPerformanceInfoRecord> records =
				GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats( dbStats );

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand() )
			{
				cmd.Connection = conn;
				cmd.CommandText = $@"INSERT INTO {TestOptions.DefaultMapping.ExecutionTimeStatsTableName} (
						et_payload_type,
						et_n_execution_cycles,
						et_last_execution_time,
						et_avg_execution_time,
						et_fastest_execution_time,
						et_longest_execution_time,
						et_total_execution_time
					) VALUES (
						@payload_type,
						@n_execution_cycles,
						@last_execution_time,
						@avg_execution_time,
						@fastest_execution_time,
						@longest_execution_time,
						@total_execution_time
					)";

				NpgsqlParameter pPayloadType = cmd.Parameters
					.Add( "payload_type", NpgsqlDbType.Varchar );
				NpgsqlParameter pNExecutionCycles = cmd.Parameters
					.Add( "n_execution_cycles", NpgsqlDbType.Bigint );
				NpgsqlParameter pLastExecutionTime = cmd.Parameters
					.Add( "last_execution_time", NpgsqlDbType.Bigint );
				NpgsqlParameter pAvgExecutionTime = cmd.Parameters
					.Add( "avg_execution_time", NpgsqlDbType.Bigint );
				NpgsqlParameter pFastestExecutionTime = cmd.Parameters
					.Add( "fastest_execution_time", NpgsqlDbType.Bigint );
				NpgsqlParameter pLongestExecutionTime = cmd.Parameters
					.Add( "longest_execution_time", NpgsqlDbType.Bigint );
				NpgsqlParameter pTotalExecutionTime = cmd.Parameters
					.Add( "total_execution_time", NpgsqlDbType.Bigint );

				await cmd.PrepareAsync();

				foreach ( ExecutionPerformanceInfoRecord r in records )
				{
					pPayloadType.Value = r.PayloadType;
					pNExecutionCycles.Value = r.NExecutionCycles;
					pLastExecutionTime.Value = r.LastExecutionTime;
					pAvgExecutionTime.Value = r.AvgExecutionTime;
					pFastestExecutionTime.Value = r.FastestExecutionTime;
					pLongestExecutionTime.Value = r.LongestExecutionTime;
					pTotalExecutionTime.Value = r.TotalExecutionTime;

					await cmd.ExecuteNonQueryAsync();
				}

				await conn.CloseAsync();
			}

			return dbStats;
		}

		private List<TaskPerformanceStats> DuplicatePayloadTypes ( List<TaskPerformanceStats> source )
		{
			Faker faker =
				new Faker();

			List<TaskPerformanceStats> result =
				new List<TaskPerformanceStats>( source );

			int nDuplicates = faker.Random.Int( 1, source.Count );

			for ( int i = 0; i < nDuplicates; i++ )
			{
				TaskPerformanceStats duplicate = faker
					.PickRandom( source );
				result.Add( new TaskPerformanceStats( duplicate.PayloadType,
					faker.Random.Long( 1, 1000000 ) ) );
			}

			return result;
		}

		private List<ExecutionPerformanceInfoRecord> GetExecutionPerformanceInfoRecordsFromTaskPeformanceStats ( params List<TaskPerformanceStats>[] statsList )
		{
			List<ExecutionPerformanceInfoRecord> perfRecords =
				new List<ExecutionPerformanceInfoRecord>();

			foreach ( List<TaskPerformanceStats> sList in statsList )
			{
				foreach ( TaskPerformanceStats s in sList )
				{
					ExecutionPerformanceInfoRecord record = perfRecords
						.FirstOrDefault( r => r.PayloadType == s.PayloadType );

					if ( record != null )
					{
						record.NExecutionCycles += 1;
						record.LastExecutionTime = s.DurationMilliseconds;
						record.TotalExecutionTime += s.DurationMilliseconds;
						record.AvgExecutionTime = ( long )Math.Ceiling( ( double )record.TotalExecutionTime
							/ record.NExecutionCycles );
						record.LongestExecutionTime = Math.Max( record.LongestExecutionTime,
							s.DurationMilliseconds );
						record.FastestExecutionTime = Math.Min( record.FastestExecutionTime,
							s.DurationMilliseconds );
					}
					else
					{
						perfRecords.Add( new ExecutionPerformanceInfoRecord()
						{
							PayloadType = s.PayloadType,
							NExecutionCycles = 1,
							AvgExecutionTime = s.DurationMilliseconds,
							FastestExecutionTime = s.DurationMilliseconds,
							LastExecutionTime = s.DurationMilliseconds,
							LongestExecutionTime = s.DurationMilliseconds,
							TotalExecutionTime = s.DurationMilliseconds
						} );
					}
				}
			}

			return perfRecords;
		}

		private async Task<List<ExecutionPerformanceInfoRecord>> GetDbTaskPerformanceStatsAsync ()
		{
			List<ExecutionPerformanceInfoRecord> dbPerfRecords =
				new List<ExecutionPerformanceInfoRecord>();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT * FROM {TestOptions.DefaultMapping.ExecutionTimeStatsTableName}", conn ) )
			{
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					while ( await rdr.ReadAsync() )
					{
						dbPerfRecords.Add( new ExecutionPerformanceInfoRecord()
						{
							PayloadType = rdr.GetString( rdr.GetOrdinal( "et_payload_type" ) ),
							NExecutionCycles = rdr.GetInt64( rdr.GetOrdinal( "et_n_execution_cycles" ) ),
							AvgExecutionTime = rdr.GetInt64( rdr.GetOrdinal( "et_avg_execution_time" ) ),
							FastestExecutionTime = rdr.GetInt64( rdr.GetOrdinal( "et_fastest_execution_time" ) ),
							LongestExecutionTime = rdr.GetInt64( rdr.GetOrdinal( "et_longest_execution_time" ) ),
							TotalExecutionTime = rdr.GetInt64( rdr.GetOrdinal( "et_total_execution_time" ) ),
							LastExecutionTime = rdr.GetInt64( rdr.GetOrdinal( "et_last_execution_time" ) )
						} );
					}
				}

				await conn.CloseAsync();
			}

			return dbPerfRecords;
		}

		private async Task CleanupPerformanceMonitorTableAsync ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"TRUNCATE TABLE {TestOptions.DefaultMapping.ExecutionTimeStatsTableName}", conn ) )
			{
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private PostgreSqlExecutionPerformanceMonitorWriter GetWriter ()
		{
			return new PostgreSqlExecutionPerformanceMonitorWriter( TestOptions
				.GetDefaultPostgreSqlExecutionPerformanceMonitorWriterOptions( ConnectionString ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
