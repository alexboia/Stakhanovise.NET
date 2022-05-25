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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	[SingleThreaded]
	public class QueueResultTableDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}


		private async Task RunDbAssetSetupTestsAsync( QueuedTaskMapping mapping )
		{
			QueueResultTableDbAssetSetup setup =
				new QueueResultTableDbAssetSetup();

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
			await AssertTableHasExpectedIndexesAsync( mapping,
				shouldHaveTaskStatusIndex: true,
				shouldHaveTaskTypeIndex: true );
		}

		private async Task AssertTableExistsAsync( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.ResultsQueueTableName );

			Assert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.ResultsQueueTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.ResultsQueueTableName,
				"task_id",
				"task_type",
				"task_status",
				"task_source",
				"task_payload",
				"task_priority",
				"task_last_error",
				"task_error_count",
				"task_last_error_is_recoverable",
				"task_processing_time_milliseconds",
				"task_posted_at_ts",
				"task_first_processing_attempted_at_ts",
				"task_last_processing_attempted_at_ts",
				"task_processing_finalized_at_ts" );

			Assert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.ResultsQueueTableName );
		}

		private async Task AssertTableHasExpectedIndexesAsync( QueuedTaskMapping mapping, bool shouldHaveTaskStatusIndex, bool shouldHaveTaskTypeIndex )
		{
			string expectedTaskStatusIndexName = string.Format( "idx_{0}_task_status",
				mapping.ResultsQueueTableName );

			bool taskStatusIndexExists = await TableIndexExistsAsync( mapping.ResultsQueueTableName,
				expectedTaskStatusIndexName );

			Assert.AreEqual( shouldHaveTaskStatusIndex,
				taskStatusIndexExists );

			string expectedTaskTypeIndexName = string.Format( "idx_{0}_task_type",
				mapping.ResultsQueueTableName );

			bool taskTypeIndexExists = await TableIndexExistsAsync( mapping.ResultsQueueTableName,
				expectedTaskTypeIndexName );

			Assert.AreEqual( shouldHaveTaskTypeIndex,
				taskTypeIndexExists );
		}

		private QueuedTaskMapping GetDefaultMapping()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.ResultsQueueTableName = RandomizeDbAssetName( mapping.ResultsQueueTableName );
			return mapping;
		}
	}
}
