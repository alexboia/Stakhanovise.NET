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
