using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using System.Linq;
using Bogus;
using MoreLinq;
using LVD.Stakhanovise.NET.Tests.Helpers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class AppMetricsCollectionTests
	{
		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetricIds_SpecificAppIds ()
		{
			AppMetricId[] expectedMetricIds = new AppMetricId[]
			{
				AppMetricId.BufferMaxCount,
				AppMetricId.BufferMinCount,
				AppMetricId.BufferTimesEmptied,
				AppMetricId.BufferTimesFilled,
				AppMetricId.ListenerNotificationWaitTimeoutCount,
				AppMetricId.ListenerReconnectCount,
				AppMetricId.ListenerTaskNotificationCount
			};

			AppMetricsCollection metrics = new AppMetricsCollection( expectedMetricIds );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				metrics );
		}

		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetricIds_AllBuiltInAppIds ()
		{
			AppMetricId[] expectedMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			AppMetricsCollection metrics = new AppMetricsCollection( expectedMetricIds );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				metrics );
		}

		[Test]
		public void Test_CanCreateAppMetricsCollection_FromAppMetrics ()
		{
			int iMetric = 0;
			Faker faker = new Faker();
			AppMetric[] expecteMetrics = new AppMetric[ AppMetricId.BuiltInAppMetricIds.Count() ];

			foreach ( AppMetricId mId in AppMetricId.BuiltInAppMetricIds )
				expecteMetrics[ iMetric++ ] = new AppMetric( mId, faker.Random.Long( 0 ) );

			AppMetricsCollection metrics = new AppMetricsCollection( expecteMetrics );

			Assert_AppMetricsCollection_CorrectlyInitialized( expecteMetrics, metrics );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanUpdateMetric_Increment ( int times )
		{
			AppMetricId[] targetMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			AppMetricsCollection metrics = new AppMetricsCollection( targetMetricIds );

			for ( int i = 0; i < times; i++ )
			{
				foreach ( AppMetricId metricId in targetMetricIds )
					metrics.UpdateMetric( metricId, m => m.Increment() );
			}

			foreach ( AppMetricId metricId in targetMetricIds )
				Assert.AreEqual( times, metrics.QueryMetric( metricId ).Value );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanUpdateMetric_Decrement ( int times )
		{
			Faker faker =
				new Faker();

			AppMetricId[] targetMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			long initialValue = times * faker.Random
				.Long( 1, 100000 );

			AppMetricsCollection metrics = new AppMetricsCollection( targetMetricIds
				.Select( mId => new AppMetric( mId, initialValue ) )
				.ToArray() );

			for ( int i = 0; i < times; i++ )
			{
				foreach ( AppMetricId metricId in targetMetricIds )
					metrics.UpdateMetric( metricId, m => m.Decrement() );
			}

			foreach ( AppMetricId metricId in targetMetricIds )
				Assert.AreEqual( initialValue - times, metrics.QueryMetric( metricId ).Value );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanUpdateMetric_Add ( int times )
		{
			Faker faker =
				new Faker();

			AppMetricId[] targetMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			long expectedTotal = 0;

			AppMetricsCollection metrics = new AppMetricsCollection( targetMetricIds );

			for ( int i = 0; i < times; i++ )
			{
				long randomValToAdd = faker.Random.Long( 0, 100000 );

				foreach ( AppMetricId metricId in targetMetricIds )
					metrics.UpdateMetric( metricId, m => m.Add( randomValToAdd ) );

				expectedTotal += randomValToAdd;
			}

			foreach ( AppMetricId metricId in targetMetricIds )
				Assert.AreEqual( expectedTotal, metrics.QueryMetric( metricId ).Value );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanUpdateMetric_ReplaceValue ( int times )
		{
			Faker faker =
				new Faker();

			AppMetricId[] targetMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			AppMetricsCollection metrics = new AppMetricsCollection( targetMetricIds );

			for ( int i = 0; i < times; i++ )
			{
				foreach ( AppMetricId metricId in targetMetricIds )
				{
					long randomNewVal = faker.Random.Long();
					metrics.UpdateMetric( metricId, m => m.Update( randomNewVal ) );

					Assert.AreEqual( randomNewVal, metrics
						.QueryMetric( metricId )
						.Value );
				}
			}
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_NoOverlappingMetricIds_DefaultInitialValues ( int nCollections )
		{
			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			int batchSize = allMetricIds.Length / nCollections;

			List<AppMetricId> expectedMetricIds =
				new List<AppMetricId>();

			AppMetricId[][] metricIdsBatches = allMetricIds
				.Batch( batchSize, b => b.ToArray() )
				.ToArray();

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				collectionBatches[ i ] = new AppMetricsCollection( metricIdsBatches[ i ] );
				expectedMetricIds.AddRange( metricIdsBatches[ i ] );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_NoOverlappingMetricIds_CustomInitialValues ( int nCollections )
		{
			Faker faker =
				new Faker();

			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			int batchSize = allMetricIds.Length / nCollections;

			List<AppMetric> expectedMetrics =
				new List<AppMetric>();

			AppMetricId[][] metricIdsBatches = allMetricIds
				.Batch( batchSize, b => b.ToArray() )
				.ToArray();

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				List<AppMetric> batchMetrics = new List<AppMetric>();

				foreach ( AppMetricId mId in metricIdsBatches[ i ] )
				{
					AppMetric newMetric = new AppMetric( mId, faker.Random.Long( 0 ) );
					expectedMetrics.Add( newMetric.Copy() );
					batchMetrics.Add( newMetric );
				}

				collectionBatches[ i ] = new AppMetricsCollection( batchMetrics
					.ToArray() );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_WithOverlappingMetricIds_DefaultInitialValues ( int nCollections )
		{
			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			int batchSize = allMetricIds.Length / nCollections;

			List<AppMetricId> expectedMetricIds =
				new List<AppMetricId>();

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.Batch( batchSize, b => b.ToList() )
				.ToArray();

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				collectionBatches[ i ] = new AppMetricsCollection( metricIdsBatches[ i ]
					.ToArray() );

				foreach ( AppMetricId mId in metricIdsBatches[ i ] )
					if ( !expectedMetricIds.Contains( mId ) )
						expectedMetricIds.Add( mId );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_WithOverlappingMetricIds_CustomInitialValues ( int nCollections )
		{
			Faker faker =
				new Faker();

			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			int batchSize = allMetricIds.Length / nCollections;

			List<AppMetric> expectedMetrics =
				new List<AppMetric>();

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.Batch( batchSize, b => b.ToList() )
				.ToArray();

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				List<AppMetric> batchMetrics = GenerateMetricsForMetricIdsBatch( metricIdsBatches[ i ],
					expectedMetrics );

				collectionBatches[ i ] = new AppMetricsCollection( batchMetrics
					.ToArray() );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_SameMetricIds_DefaultInitialValues ( int nCollections )
		{
			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			List<AppMetricId> expectedMetricIds =
				new List<AppMetricId>( allMetricIds );

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				collectionBatches[ i ] = new AppMetricsCollection( metricIdsBatches[ i ]
					.ToArray() );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_SameMetricIds_CustomInitialValues ( int nCollections )
		{
			Faker faker =
				new Faker();

			AppMetricId[] allMetricIds = AppMetricId
				.BuiltInAppMetricIds
				.ToArray();

			List<AppMetric> expectedMetrics =
				new List<AppMetric>();

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				List<AppMetric> batchMetrics = GenerateMetricsForMetricIdsBatch( metricIdsBatches[ i ], 
					expectedMetrics );

				collectionBatches[ i ] = new AppMetricsCollection( batchMetrics
					.ToArray() );
			}

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collectionBatches );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetricId> expectedMetricIds, AppMetricsCollection metrics )
		{
			IEnumerable<AppMetricId> actualMetricIds = metrics
				.ExportedMetrics;

			Assert.AreEqual( expectedMetricIds.Count(),
				actualMetricIds.Count() );

			foreach ( AppMetricId metricId in expectedMetricIds )
				CollectionAssert.Contains( actualMetricIds, metricId );

			foreach ( AppMetricId id in expectedMetricIds )
			{
				AppMetric metric = metrics.QueryMetric( id );
				Assert.NotNull( metric );
				Assert.AreEqual( 0, metric.Value );
			}

			Assert.AreEqual( expectedMetricIds.Count(), metrics
				.CollectMetrics()
				.Count() );

			foreach ( AppMetric metric in metrics.CollectMetrics() )
				Assert.AreEqual( 0, metric.Value );
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetric> expectedMetrics, AppMetricsCollection metrics )
		{
			IEnumerable<AppMetricId> actualMetricIds = metrics
				.ExportedMetrics;

			Assert.AreEqual( expectedMetrics.Count(),
				actualMetricIds.Count() );

			foreach ( AppMetric expectedMetric in expectedMetrics )
				CollectionAssert.Contains( actualMetricIds, expectedMetric.Id );

			foreach ( AppMetric m in expectedMetrics )
			{
				AppMetric metric = metrics.QueryMetric( m.Id );
				Assert.NotNull( metric );
				Assert.AreEqual( m.Value, metric.Value );
			}

			Assert.AreEqual( expectedMetrics.Count(), metrics
				.CollectMetrics()
				.Count() );
		}

		private List<AppMetric> GenerateMetricsForMetricIdsBatch ( List<AppMetricId> forMetricIdsBatch,
			List<AppMetric> expectedMetrics )
		{
			Faker faker = new Faker();

			List<AppMetric> batchMetrics =
				new List<AppMetric>();

			foreach ( AppMetricId mId in forMetricIdsBatch )
			{
				AppMetric newMetric = new AppMetric( mId, faker.Random.Long( 0 ) );
				batchMetrics.Add( newMetric );

				AppMetric existingMetric = expectedMetrics.FirstOrDefault( m => m.Id.Equals( mId ) );
				if ( existingMetric != null )
					existingMetric.Add( newMetric.Value );
				else
					expectedMetrics.Add( newMetric.Copy() );
			}

			return batchMetrics;
		}

		private List<AppMetricId>[] InterleaveAppMetricIdsBatches ( List<AppMetricId>[] metricIdsBatches )
		{
			Faker faker =
				new Faker();

			foreach ( List<AppMetricId> batch in metricIdsBatches )
			{
				List<AppMetricId> pickFromBatch = faker.PickRandomWithout( metricIdsBatches, batch );

				foreach ( AppMetricId mId in pickFromBatch )
				{
					if ( !batch.Contains( mId ) )
					{
						batch.Add( mId );
						break;
					}
				}
			}

			return metricIdsBatches;
		}
	}
}
