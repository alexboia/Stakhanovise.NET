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
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			List<AppMetricId> expectedMetricIds =
				GetUniqueMetricIds( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

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

			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithCustomValues( metricIdsBatches );

			List<AppMetric> expectedMetrics =
				MergeAppMetrics( collections );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_WithOverlappingMetricIds_DefaultInitialValues ( int nCollections )
		{
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			List<AppMetricId> expectedMetricIds =
				GetUniqueMetricIds( metricIdsBatches );

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_WithOverlappingMetricIds_CustomInitialValues ( int nCollections )
		{
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithCustomValues( metricIdsBatches );

			List<AppMetric> expectedMetrics =
				MergeAppMetrics( collections );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_SameMetricIds_DefaultInitialValues ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			List<AppMetricId> expectedMetricIds =
				new List<AppMetricId>( allMetricIds );

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetricIds,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_JoinMetricsFromProviders_SameMetricIds_CustomInitialValues ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithCustomValues( metricIdsBatches );

			List<AppMetric> expectedMetrics =
				MergeAppMetrics( collections );

			AppMetricsCollection finalCollection = AppMetricsCollection
				.JoinProviders( collections );

			Assert_AppMetricsCollection_CorrectlyInitialized( expectedMetrics,
				finalCollection );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanJoinExportedMetrics_NoOverlappingMetricIds ( int nCollections )
		{
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			List<AppMetricId> expectedMetricIds =
				GetUniqueMetricIds( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			IEnumerable<AppMetricId> metricIds = AppMetricsCollection
				.JoinExportedMetrics( collections );

			Assert.NotNull( metricIds );
			CollectionAssert.AreEquivalent( expectedMetricIds,
				metricIds );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanJoinExportedMetrics_WithOverlappingMetricIds ( int nCollections )
		{
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			List<AppMetricId> expectedMetricIds =
				GetUniqueMetricIds( metricIdsBatches );

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			IEnumerable<AppMetricId> metricIds = AppMetricsCollection
				.JoinExportedMetrics( collections );

			Assert.NotNull( metricIds );
			CollectionAssert.AreEquivalent( expectedMetricIds,
				metricIds );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanJoinExportedMetrics_SameMetricIds ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			List<AppMetricId> expectedMetricIds =
				new List<AppMetricId>( allMetricIds );

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			IEnumerable<AppMetricId> metricIds = AppMetricsCollection
				.JoinExportedMetrics( collections );

			Assert.NotNull( metricIds );
			CollectionAssert.AreEquivalent( expectedMetricIds,
				metricIds );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public void Test_CanJoinQueryMetric_PresentInAllProviders ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.MultiplyCollection( nCollections );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithCustomValues( metricIdsBatches );

			Assert_CorrectJoinQueryAppMetrics( allMetricIds,
				collections );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanJoinQueryMetric_PresentInSomeProviders ( int nCollections )
		{
			List<AppMetricId>[] metricIdsBatches =
				GetAppMetricIdsBatches( nCollections );

			metricIdsBatches = InterleaveAppMetricIdsBatches( metricIdsBatches );

			AppMetricsCollection[] collections =
				GetCollectionsFromMetricIdsBatchesWithInitialValues( metricIdsBatches );

			List<AppMetricId> checkAppMetricIds =
				GetUniqueMetricIds( metricIdsBatches );

			Assert_CorrectJoinQueryAppMetrics( checkAppMetricIds,
				collections );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public void Test_CanJoinQueryMetric_AbsentFromAllProviders ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			foreach ( AppMetricId testMetricId in allMetricIds )
			{
				AppMetricId[] metricIds = AllBuiltInMetricIds
					.Where( m => !m.Equals( testMetricId ) )
					.ToArray();

				List<AppMetricId>[] metricIdsBatches = metricIds
					.MultiplyCollection( nCollections );

				AppMetricsCollection[] collections = 
					GetCollectionsFromMetricIdsBatchesWithCustomValues( metricIdsBatches );

				AppMetric testMetric = AppMetricsCollection.JoinQueryMetric( testMetricId, 
					collections );

				Assert.IsNull( testMetric );
			}
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetricId> expectedMetricIds,
			AppMetricsCollection metrics )
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

		private void Assert_CorrectJoinQueryAppMetrics ( IEnumerable<AppMetricId> checkMetricIds,
			AppMetricsCollection[] collectionBatches )
		{
			foreach ( AppMetricId metricId in checkMetricIds )
			{
				long expectedValue = SumMetricValues( collectionBatches,
					metricId );

				AppMetric metric = AppMetricsCollection.JoinQueryMetric( metricId,
					collectionBatches );

				Assert.NotNull( metric );
				Assert.AreEqual( expectedValue, metric.Value );
			}
		}

		private void Assert_AppMetricsCollection_CorrectlyInitialized ( IEnumerable<AppMetric> expectedMetrics,
			AppMetricsCollection metrics )
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

		private List<AppMetricId>[] GetAppMetricIdsBatches ( int nCollections )
		{
			AppMetricId[] allMetricIds = AllBuiltInMetricIds;

			int batchSize = allMetricIds.Length / nCollections;

			List<AppMetricId>[] metricIdsBatches = allMetricIds
				.Batch( batchSize, b => b.ToList() )
				.ToArray();

			return metricIdsBatches;
		}

		private List<AppMetricId> GetUniqueMetricIds ( List<AppMetricId>[] metricIdsBatches )
		{
			List<AppMetricId> uniqueMetricIds =
				new List<AppMetricId>();

			metricIdsBatches.ForEach( batch =>
			{
				batch.ForEach( mId =>
				{
					if ( !uniqueMetricIds.Contains( mId ) )
						uniqueMetricIds.Add( mId );
				} );
			} );

			return uniqueMetricIds;
		}

		private List<AppMetric> MergeAppMetrics ( AppMetricsCollection[] collections )
		{
			List<AppMetric> metrics =
				new List<AppMetric>();

			foreach ( AppMetricsCollection col in collections )
			{
				foreach ( AppMetric metric in col.CollectMetrics() )
				{
					AppMetric existingMetric = metrics.FirstOrDefault( m
						=> m.Id.Equals( metric.Id ) );

					if ( existingMetric != null )
						existingMetric.Add( metric.Value );
					else
						metrics.Add( metric.Copy() );
				}
			}

			return metrics;
		}

		private AppMetricsCollection[] GetCollectionsFromMetricIdsBatchesWithInitialValues ( List<AppMetricId>[] metricIdsBatches )
		{
			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				collectionBatches[ i ] = new AppMetricsCollection( metricIdsBatches[ i ]
					.ToArray() );
			}

			return collectionBatches;
		}

		private AppMetricsCollection[] GetCollectionsFromMetricIdsBatchesWithCustomValues ( List<AppMetricId>[] metricIdsBatches )
		{
			Faker faker =
				new Faker();

			AppMetricsCollection[] collectionBatches =
				new AppMetricsCollection[ metricIdsBatches.Length ];

			for ( int i = 0; i < metricIdsBatches.Length; i++ )
			{
				List<AppMetric> metrics = faker.RandomAppMetrics( metricIdsBatches[ i ],
					minValue: 0,
					maxValue: 100000 );

				collectionBatches[ i ] = new AppMetricsCollection( metrics
					.ToArray() );
			}

			return collectionBatches;
		}

		public long SumMetricValues ( AppMetricsCollection[] collections, AppMetricId metricId )
		{
			long value = 0;

			foreach ( AppMetricsCollection col in collections )
			{
				foreach ( AppMetric m in col.CollectMetrics() )
				{
					if ( m.Id.Equals( metricId ) )
					{
						value += m.Value;
						break;
					}
				}
			}

			return value;
		}

		AppMetricId[] AllBuiltInMetricIds => AppMetricId
			.BuiltInAppMetricIds
			.ToArray();
	}
}
