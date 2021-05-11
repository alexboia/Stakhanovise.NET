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
	public class ExecutionTimeStatsTableDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		private async Task RunDbAssetSetupTestsAsync ( QueuedTaskMapping mapping )
		{
			ExecutionTimeStatsTableDbAssetSetup setup =
				new ExecutionTimeStatsTableDbAssetSetup();

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
		}

		private async Task AssertTableExistsAsync ( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.ExecutionTimeStatsTableName );

			Assert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.ExecutionTimeStatsTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync ( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.ExecutionTimeStatsTableName,
				ExecutionTimeStatsTableDbAssetSetup.PayloadTypeColumnName,
				ExecutionTimeStatsTableDbAssetSetup.NExecutionCyclesColumnName,
				ExecutionTimeStatsTableDbAssetSetup.LastExecutionTimeColumnName,
				ExecutionTimeStatsTableDbAssetSetup.AverageExecutionTimeColumnName,
				ExecutionTimeStatsTableDbAssetSetup.FastestExecutionTimeColumnName,
				ExecutionTimeStatsTableDbAssetSetup.LongestExecutionTimeColumnName,
				ExecutionTimeStatsTableDbAssetSetup.TotalExecutionTimeColumnName );

			Assert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.ExecutionTimeStatsTableName );
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.ExecutionTimeStatsTableName = RandomizeDbAssetName( mapping.ExecutionTimeStatsTableName );
			return mapping;
		}
	}
}
