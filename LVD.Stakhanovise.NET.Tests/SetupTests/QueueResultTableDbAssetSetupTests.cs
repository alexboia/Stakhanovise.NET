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
	public class QueueResultTableDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping_WithIndexes ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withTaskStatusIndex: true,
				withTaskTypeIndex: true );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping_WithoutIndexes ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withTaskStatusIndex: false,
				withTaskTypeIndex: false );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping_WithIndexes ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withTaskStatusIndex: true,
				withTaskTypeIndex: true );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping_WithoutIndexes ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping,
				withTaskStatusIndex: false,
				withTaskTypeIndex: false );
		}

		private async Task RunDbAssetSetupTestsAsync ( QueuedTaskMapping mapping, bool withTaskStatusIndex, bool withTaskTypeIndex )
		{
			QueueResultTableDbAssetSetup setup =
				new QueueResultTableDbAssetSetup( withTaskStatusIndex, withTaskTypeIndex );

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
			await AssertTableHasExpectedIndexesAsync( mapping,
				shouldHaveTaskStatusIndex: withTaskStatusIndex,
				shouldHaveTaskTypeIndex: withTaskTypeIndex );
		}

		private async Task AssertTableExistsAsync ( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.ResultsQueueTableName );

			Assert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.ResultsQueueTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync ( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.ResultsQueueTableName,
				QueueResultTableDbAssetSetup.TaskIdColumnName,
				QueueResultTableDbAssetSetup.TaskTypeColumnName,
				QueueResultTableDbAssetSetup.TaskStatusColumnName,
				QueueResultTableDbAssetSetup.TaskSourceColumnName,
				QueueResultTableDbAssetSetup.TaskPayloadColumnName,
				QueueResultTableDbAssetSetup.TaskPriorityColumnName,
				QueueResultTableDbAssetSetup.TaskLastErrorColumnName,
				QueueResultTableDbAssetSetup.TaskErrorCountColumnName,
				QueueResultTableDbAssetSetup.TaskLastErrorIsRecoverableColumnName,
				QueueResultTableDbAssetSetup.TaskProcessingTimeMillisecondsColumnName,
				QueueResultTableDbAssetSetup.TaskPostedAtColumnName,
				QueueResultTableDbAssetSetup.TaskFirstProcessingAttemptedAtColumnName,
				QueueResultTableDbAssetSetup.TaskLastProcessingAttemptedAtColumnName,
				QueueResultTableDbAssetSetup.TaskProcessingFinalizedAtColumnName );

			Assert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.ResultsQueueTableName );
		}

		private async Task AssertTableHasExpectedIndexesAsync ( QueuedTaskMapping mapping, bool shouldHaveTaskStatusIndex, bool shouldHaveTaskTypeIndex )
		{
			string expectedTaskStatusIndexName = string.Format( QueueResultTableDbAssetSetup.TaskStatusIndexNameFormat,
				mapping.ResultsQueueTableName );

			bool taskStatusIndexExists = await TableIndexExistsAsync( mapping.ResultsQueueTableName,
				expectedTaskStatusIndexName );

			Assert.AreEqual( shouldHaveTaskStatusIndex,
				taskStatusIndexExists );

			string expectedTaskTypeIndexName = string.Format( QueueResultTableDbAssetSetup.TaskTypeIndexNameFormat,
				mapping.ResultsQueueTableName );

			bool taskTypeIndexExists = await TableIndexExistsAsync( mapping.ResultsQueueTableName,
				expectedTaskTypeIndexName );

			Assert.AreEqual( shouldHaveTaskTypeIndex,
				taskTypeIndexExists );
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.ResultsQueueTableName = RandomizeDbAssetName( mapping.ResultsQueueTableName );
			return mapping;
		}
	}
}
