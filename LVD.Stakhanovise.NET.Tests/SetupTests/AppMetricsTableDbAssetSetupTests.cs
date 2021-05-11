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
	public class AppMetricsTableDbAssetSetupTests : BaseSetupDbTests
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
			AppMetricsTableDbAssetSetup setup =
				new AppMetricsTableDbAssetSetup();

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
			await AssertTableHasExpectedIndexesAsync( mapping );
		}

		private async Task AssertTableExistsAsync ( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.MetricsTableName );

			Assert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.MetricsTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync ( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.MetricsTableName,
				AppMetricsTableDbAssetSetup.MetricIdColumnName,
				AppMetricsTableDbAssetSetup.MetricCategoryColumnName,
				AppMetricsTableDbAssetSetup.MetricLastUpdatedColumnName,
				AppMetricsTableDbAssetSetup.MetricValueColumnName );

			Assert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.MetricsTableName );
		}

		private async Task AssertTableHasExpectedIndexesAsync ( QueuedTaskMapping mapping )
		{
			string expectedMetricCategoryIndexName = string.Format( AppMetricsTableDbAssetSetup.MetricCategoryIndexFormat,
				mapping.MetricsTableName );

			bool metricCategoryIndexExists = await TableIndexExistsAsync( mapping.MetricsTableName,
				expectedMetricCategoryIndexName );

			Assert.IsTrue( metricCategoryIndexExists,
				"Table {0} does not have expected index {1}",
				mapping.MetricsTableName,
				expectedMetricCategoryIndexName );
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping ()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.MetricsTableName = RandomizeDbAssetName( mapping.MetricsTableName );
			return mapping;
		}
	}
}
