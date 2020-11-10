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

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlAppMetricsMonitorWriterTests : BaseDbTests
	{
		[SetUp]
		public async Task Setup ()
		{
			await ClearMetricsTableAsync();
		}

		[TearDown]
		public async Task TearDown ()
		{
			await ClearMetricsTableAsync();
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanWrite_NoInitialAppMetrics_AllBuiltInAppMetricIds ()
		{
			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public async Task Test_CanWrite_DisjointSets ( int nSets )
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
				await writer.WriteAsync( writeList );
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
		public async Task Test_CanWrite_PartiallyIntersectedSets ( int nSets )
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
				await writer.WriteAsync( writeList );
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
		public async Task Test_CanWriteEmptyData_NoInitialAppMetrics ()
		{
			PostgreSqlAppMetricsMonitorWriter writer = GetWriter();
			List<AppMetric> noMetrics = new List<AppMetric>();

			await writer.WriteAsync( noMetrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( noMetrics,
				dbMetrics );
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanWrite_WithInitialAppMetrics_AllBuiltInAppMetricIds ()
		{
			await GenerateAppMetricsInDbForBuiltInAppMetricIds();

			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();
			List<AppMetric> metrics =
				GenerateAppMetricsForBuiltInAppMetricIds();

			await writer.WriteAsync( metrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( metrics,
				dbMetrics );
		}

		[Test]
		public async Task Test_CanWriteEmptyData_WithInitialAppMetrics ()
		{
			await GenerateAppMetricsInDbForBuiltInAppMetricIds();

			List<AppMetric> expectedDbMetrics =
				await GetDbAppMetricsAsync();

			PostgreSqlAppMetricsMonitorWriter writer =
				GetWriter();

			List<AppMetric> noMetrics =
				new List<AppMetric>();

			await writer.WriteAsync( noMetrics );

			List<AppMetric> dbMetrics =
				await GetDbAppMetricsAsync();

			CollectionAssert.AreEquivalent( expectedDbMetrics,
				dbMetrics );
		}

		private List<List<AppMetric>> InterleaveAppMetricLists ( List<List<AppMetric>> appMetricLists )
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

		private async Task<List<AppMetric>> GetDbAppMetricsAsync ()
		{
			List<AppMetric> dbMetrics =
				new List<AppMetric>();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT * from {TestOptions.DefaultMapping.MetricsTableName}", conn ) )
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

		private async Task GenerateAppMetricsInDbForBuiltInAppMetricIds ()
		{
			Faker faker =
				new Faker();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand() )
			{
				cmd.Connection = conn;
				cmd.CommandText = $@"INSERT INTO {TestOptions.DefaultMapping.MetricsTableName} (
						metric_id,
						metric_category,
						metric_value,
						metric_last_updated
					) VALUES (
						@m_id,
						@m_category,
						@m_value,
						NOW()
					)";

				NpgsqlParameter pMetricId = cmd.Parameters.Add( "m_id",
					NpgsqlDbType.Varchar );
				NpgsqlParameter pMetricCategory = cmd.Parameters.Add( "m_category",
					NpgsqlDbType.Varchar );
				NpgsqlParameter pMetricValue = cmd.Parameters.Add( "m_value",
					NpgsqlDbType.Bigint );

				foreach ( AppMetricId mId in AllBuiltInMetricIds )
				{
					pMetricId.Value = mId.ValueId;
					pMetricCategory.Value = mId.ValueCategory;
					pMetricValue.Value = faker.Random.Long( 0 );

					await cmd.ExecuteNonQueryAsync();
				}
			}
		}

		private async Task ClearMetricsTableAsync ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"TRUNCATE TABLE {TestOptions.DefaultMapping.MetricsTableName}", conn ) )
			{
				await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private List<AppMetric> GenerateAppMetricsForBuiltInAppMetricIds ()
		{
			Faker faker =
				new Faker();
			List<AppMetric> appMetrics =
				new List<AppMetric>();

			foreach ( AppMetricId mId in AllBuiltInMetricIds )
				appMetrics.Add( faker.RandomAppMetric( mId ) );

			return appMetrics;
		}

		private PostgreSqlAppMetricsMonitorWriter GetWriter ()
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
