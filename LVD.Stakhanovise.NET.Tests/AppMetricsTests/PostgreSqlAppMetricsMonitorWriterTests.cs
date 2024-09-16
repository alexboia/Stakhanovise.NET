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
using MoreLinq;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Tests.AppMetricsTests
{
	[TestFixture]
	public class PostgreSqlAppMetricsMonitorWriterTests : BaseDbTests
	{
		private string mTestProcessId;

		public PostgreSqlAppMetricsMonitorWriterTests()
			: base()
		{
			mTestProcessId = Guid.NewGuid()
				.ToString();
		}

		[SetUp]
		public async Task Setup()
		{
			await ClearMetricsTableAsync();
		}

		[TearDown]
		public async Task TearDown()
		{
			await ClearMetricsTableAsync();
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanWrite_NoInitialAppMetrics_AllBuiltInAppMetricIds()
		{
			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( mTestProcessId, metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public async Task Test_CanWrite_DisjointSets( int nSets )
		{
			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();

			List<AppMetric> allMetrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			List<List<AppMetric>> writeMetricLists = allMetrics
				.Batch( nSets, s => s.ToList() )
				.ToList();

			List<AppMetric> expectedMetrics =
				new List<AppMetric>();

			foreach ( List<AppMetric> writeList in writeMetricLists )
			{
				await writer.WriteAsync( mTestProcessId, writeList );
				expectedMetrics.AddRange( writeList );
			}

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( expectedMetrics,
				dbMetrics );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public async Task Test_CanWrite_PartiallyIntersectedSets( int nSets )
		{
			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();

			List<AppMetric> allMetrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			List<List<AppMetric>> writeMetricLists = InterleaveAppMetricLists( allMetrics
				.Batch( nSets, s => s.ToList() )
				.ToList() );

			List<AppMetric> expectedMetrics =
				new List<AppMetric>();

			foreach ( List<AppMetric> writeList in writeMetricLists )
			{
				await writer.WriteAsync( mTestProcessId, writeList );
				foreach ( AppMetric wMetric in writeList )
				{
					int indexOfWMetric = expectedMetrics.FindIndex( m => m.Id.Equals( wMetric.Id ) );
					if ( indexOfWMetric >= 0 )
						expectedMetrics[ indexOfWMetric ] = wMetric;
					else
						expectedMetrics.Add( wMetric );
				}
			}

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( expectedMetrics,
				dbMetrics );
		}

		[Test]
		public async Task Test_CanWriteEmptyData_NoInitialAppMetrics()
		{
			PostgreSqlAppMetricsMonitorWriter writer = GetWriter();
			List<AppMetric> noMetrics = new List<AppMetric>();

			await writer.WriteAsync( mTestProcessId, noMetrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( noMetrics,
				dbMetrics );
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanWrite_WithInitialAppMetrics_AllBuiltInAppMetricIds()
		{
			await GenerateAppMetricsInDbForBuiltInAppMetricIdsAsync( allZeroValues: false );

			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( mTestProcessId, metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );
		}

		[Test]
		public async Task Test_CanWriteEmptyData_WithInitialAppMetrics()
		{
			await GenerateAppMetricsInDbForBuiltInAppMetricIdsAsync( allZeroValues: false );

			List<AppMetric> expectedDbMetrics =
				await GetDbAppMetricsAsync();

			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();

			List<AppMetric> noMetrics =
				new List<AppMetric>();

			await writer.WriteAsync( mTestProcessId, noMetrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( expectedDbMetrics,
				dbMetrics );
		}

		[Test]
		public async Task Test_CanWrite_AllZeroValues_NoInitialAppMetrics()
		{
			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAllZeroAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( mTestProcessId, metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );

			AssertAppMetricsAreAllZero( dbMetrics );
		}

		[Test]
		public async Task Test_CanWrite_AllZeroValues_WithInitialAppMetrics()
		{
			await GenerateAppMetricsInDbForBuiltInAppMetricIdsAsync( allZeroValues: true );

			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAllZeroAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( mTestProcessId, metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );

			AssertAppMetricsAreAllZero( dbMetrics );
		}

		private static void AssertAppMetricsAreAllZero( List<AppMetric> dbMetrics )
		{
			foreach ( AppMetric m in dbMetrics )
				ClassicAssert.AreEqual( 0, m.Value );
		}

		private List<List<AppMetric>> InterleaveAppMetricLists( List<List<AppMetric>> appMetricLists )
		{
			Faker faker =
				new Faker();

			foreach ( List<AppMetric> metricsList in appMetricLists )
			{
				List<AppMetric> pickFromBatch = faker.PickRandomWithout( appMetricLists, metricsList );

				foreach ( AppMetric metric in pickFromBatch )
				{
					if ( !metricsList.Any( m => m.Id.Equals( metric.Id ) ) )
					{
						metricsList.Add( metric );
						break;
					}
				}
			}

			return appMetricLists;
		}

		private async Task<List<AppMetric>> GetDbAppMetricsAsync()
		{
			List<AppMetric> dbMetrics =
				new List<AppMetric>();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT * from {TestOptions.DefaultMapping.MetricsTableName} WHERE metric_owner_process_id = '{mTestProcessId}'", conn ) )
			{
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					while ( await rdr.ReadAsync() )
					{
						AppMetricId mId = new AppMetricId(
							rdr.GetString( rdr.GetOrdinal( "metric_id" ) ),
							rdr.GetString( rdr.GetOrdinal( "metric_category" ) )
						);

						AppMetric metric = new AppMetric( mId,
							rdr.GetInt64( rdr.GetOrdinal( "metric_value" ) ) );

						dbMetrics.Add( metric );
					}
				}

				await conn.CloseAsync();
			}

			return dbMetrics;
		}

		private async Task GenerateAppMetricsInDbForBuiltInAppMetricIdsAsync( bool allZeroValues )
		{
			Faker faker =
				new Faker();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand() )
			{
				cmd.Connection = conn;
				cmd.CommandText = $@"INSERT INTO {TestOptions.DefaultMapping.MetricsTableName} (
						metric_id,
						metric_owner_process_id,
						metric_category,
						metric_value,
						metric_last_updated
					) VALUES (
						@m_id,
						@m_owner_process_id,
						@m_category,
						@m_value,
						NOW()
					)";

				NpgsqlParameter pMetricId = cmd.Parameters.Add( "m_id",
					NpgsqlDbType.Varchar );
				NpgsqlParameter pMetricOwnerProcessId = cmd.Parameters.Add( "m_owner_process_id",
					NpgsqlDbType.Varchar );
				NpgsqlParameter pMetricCategory = cmd.Parameters.Add( "m_category",
					NpgsqlDbType.Varchar );
				NpgsqlParameter pMetricValue = cmd.Parameters.Add( "m_value",
					NpgsqlDbType.Bigint );

				await cmd.PrepareAsync();

				foreach ( AppMetricId mId in AllBuiltInMetricIds )
				{
					pMetricId.Value = mId.ValueId;
					pMetricOwnerProcessId.Value = mTestProcessId;
					pMetricCategory.Value = mId.ValueCategory;
					pMetricValue.Value = !allZeroValues
						? faker.Random.Long( 0 )
						: 0;

					await cmd.ExecuteNonQueryAsync();
				}

				await conn.CloseAsync();
			}
		}

		private async Task ClearMetricsTableAsync()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"TRUNCATE TABLE {TestOptions.DefaultMapping.MetricsTableName}", conn ) )
			{
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private List<AppMetric> GenerateAppMetricsForBuiltInAppMetricIds()
		{
			Faker faker =
				new Faker();
			List<AppMetric> appMetrics =
				new List<AppMetric>();

			foreach ( AppMetricId mId in AllBuiltInMetricIds )
				appMetrics.Add( faker.RandomAppMetric( mId ) );

			return appMetrics;
		}

		private List<AppMetric> GenerateAllZeroAppMetricsForBuiltInAppMetricIds()
		{
			Faker faker =
				new Faker();
			List<AppMetric> appMetrics =
				new List<AppMetric>();

			foreach ( AppMetricId mId in AllBuiltInMetricIds )
				appMetrics.Add( new AppMetric( mId, 0 ) );

			return appMetrics;
		}

		private PostgreSqlAppMetricsMonitorWriter GetWriter()
		{
			return new PostgreSqlAppMetricsMonitorWriter( TestOptions
				.GetDefaultPostgreSqlAppMetricsMonitorWriterOptions( ConnectionString ) );
		}

		private AppMetricId[] AllBuiltInMetricIds => AppMetricId
			.BuiltInAppMetricIds
			.ToArray();

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
