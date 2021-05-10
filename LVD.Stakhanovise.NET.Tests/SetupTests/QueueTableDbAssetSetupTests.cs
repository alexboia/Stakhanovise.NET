using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using NUnit.Framework;
using Bogus;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	[SingleThreaded]
	public class QueueTableDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping_WithIndexes ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping, 
				withSortIndex: true, 
				withFilterIndex: true );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping_WithoutIndexes ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withSortIndex: false,
				withFilterIndex: false );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping_WithIndexes ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withSortIndex: true,
				withFilterIndex: true );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping_WithoutIndexes ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withSortIndex: false,
				withFilterIndex: false );
		}

		private async Task RunDbAssetSetupTestsAsync ( QueuedTaskMapping mapping, bool withSortIndex, bool withFilterIndex )
		{
			QueueTableDbAssetSetup setup =
				new QueueTableDbAssetSetup( withSortIndex, withFilterIndex );

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
			await AssertTableHasExpectedIndexesAsync( mapping,
				shouldHaveSortIndex: withSortIndex,
				shouldHaveFilterIndex: withFilterIndex );

			await AssertExpectedSequencesExistAsync( mapping );
		}

		private async Task AssertTableExistsAsync ( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.QueueTableName );

			Assert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.QueueTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync ( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.QueueTableName,
				QueueTableDbAssetSetup.TaskIdColumnName,
				QueueTableDbAssetSetup.TaskLocKHandleIdColumnName,
				QueueTableDbAssetSetup.TaskTypeColumnName,
				QueueTableDbAssetSetup.TaskSourceColumnName,
				QueueTableDbAssetSetup.TaskPayloadColumnName,
				QueueTableDbAssetSetup.TaskPriorityColumnName,
				QueueTableDbAssetSetup.TaskPostedAtColumnName,
				QueueTableDbAssetSetup.TaskLockedUntilColumnName );

			Assert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.QueueTableName );
		}

		private async Task AssertTableHasExpectedIndexesAsync ( QueuedTaskMapping mapping, bool shouldHaveSortIndex, bool shouldHaveFilterIndex )
		{
			string expectedSortIndexName = string.Format( QueueTableDbAssetSetup.SortIndexNameFormat,
				mapping.QueueTableName );

			bool sortIndexExists = await TableIndexExistsAsync( mapping.QueueTableName,
				expectedSortIndexName );

			Assert.AreEqual( shouldHaveSortIndex,
				sortIndexExists );

			string expectedFilterIndexName = string.Format( QueueTableDbAssetSetup.FilterIndexNameFormat,
				mapping.QueueTableName );

			bool filterIndexExists = await TableIndexExistsAsync( mapping.QueueTableName,
				expectedFilterIndexName );

			Assert.AreEqual( shouldHaveFilterIndex,
				filterIndexExists );
		}

		private async Task AssertExpectedSequencesExistAsync(QueuedTaskMapping mapping)
		{
			string sequenceName = string.Format( QueueTableDbAssetSetup.LockHandleIdSequenceNameFormat, 
				mapping.QueueTableName );

			bool sequenceExists = await SequenceExistsAsync( sequenceName );

			Assert.IsTrue( sequenceExists, 
				"Expected sequence {0} does not exist!", 
				sequenceName );
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.QueueTableName = RandomizeTableName( mapping.QueueTableName );
			return mapping;
		}
	}
}
