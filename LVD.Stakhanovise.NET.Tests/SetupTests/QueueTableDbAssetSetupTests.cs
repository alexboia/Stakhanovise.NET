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
using NUnit.Framework.Legacy;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	[SingleThreaded]
	public class QueueTableDbAssetSetupTests : BaseSetupDbTests
	{
		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithDefaultMapping_WithIndexes()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		[Test]
		[Repeat( 5 )]
		[NonParallelizable]
		public async Task Test_CanSetupDbAsset_WithNonDefaultMapping_WithIndexes()
		{
			QueuedTaskMapping mapping = GenerateNonDefaultMapping();
			await RunDbAssetSetupTestsAsync( mapping );
		}

		private async Task RunDbAssetSetupTestsAsync( QueuedTaskMapping mapping )
		{
			QueueTableDbAssetSetup setup =
				new QueueTableDbAssetSetup();

			await setup.SetupDbAssetAsync( GetSetupTestDbConnectionOptions(),
				mapping );

			await AssertTableExistsAsync( mapping );
			await AssertTableHasExpectedColumnsAsync( mapping );
			await AssertTableHasExpectedIndexesAsync( mapping,
				shouldHaveSortIndex: true,
				shouldHaveFilterIndex: true );

			await AssertExpectedSequencesExistAsync( mapping );
		}

		private async Task AssertTableExistsAsync( QueuedTaskMapping mapping )
		{
			bool tableExists = await TableExistsAsync( mapping.QueueTableName );

			ClassicAssert.IsTrue( tableExists,
				"Table {0} does not exist!",
				mapping.QueueTableName );
		}

		private async Task AssertTableHasExpectedColumnsAsync( QueuedTaskMapping mapping )
		{
			bool tableHasColumns = await TableHasColumnsAsync( mapping.QueueTableName,
				"task_id",
				"task_lock_handle_id",
				"task_type",
				"task_source",
				"task_payload",
				"task_priority",
				"task_posted_at_ts",
				"task_locked_until_ts" );

			ClassicAssert.IsTrue( tableHasColumns,
				"Table {0} does not have all expected columns!",
				mapping.QueueTableName );
		}

		private async Task AssertTableHasExpectedIndexesAsync( QueuedTaskMapping mapping, bool shouldHaveSortIndex, bool shouldHaveFilterIndex )
		{
			string expectedSortIndexName = string.Format( "idx_{0}_sort_index",
				mapping.QueueTableName );

			bool sortIndexExists = await TableIndexExistsAsync( mapping.QueueTableName,
				expectedSortIndexName );

			ClassicAssert.AreEqual( shouldHaveSortIndex,
				sortIndexExists );

			string expectedFilterIndexName = string.Format( "idx_{0}_filter_index",
				mapping.QueueTableName );

			bool filterIndexExists = await TableIndexExistsAsync( mapping.QueueTableName,
				expectedFilterIndexName );

			ClassicAssert.AreEqual( shouldHaveFilterIndex,
				filterIndexExists );
		}

		private async Task AssertExpectedSequencesExistAsync( QueuedTaskMapping mapping )
		{
			string sequenceName = string.Format( "{0}_task_lock_handle_id_seq",
				mapping.QueueTableName );

			bool sequenceExists = await SequenceExistsAsync( sequenceName );

			ClassicAssert.IsTrue( sequenceExists,
				"Expected sequence {0} does not exist!",
				sequenceName );
		}

		private QueuedTaskMapping GetDefaultMapping()
		{
			return new QueuedTaskMapping();
		}

		private QueuedTaskMapping GenerateNonDefaultMapping()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.QueueTableName = RandomizeDbAssetName( mapping.QueueTableName );
			return mapping;
		}
	}
}
