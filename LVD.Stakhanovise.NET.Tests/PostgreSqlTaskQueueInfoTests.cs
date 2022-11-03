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
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: also test dequeue with tasks still being locked
	[TestFixture]
	public class PostgreSqlTaskQueueInfoTests : BaseTestWithConfiguration
	{
		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueInfoTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mDataSource = new TaskQueueDataSource( mInfoOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[SetUp]
		public async Task TestSetUp ()
		{
			await mDataSource.SeedData();
		}

		[TearDown]
		public async Task TestTearDown ()
		{
			await mDataSource.ClearData();
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public async Task Test_CanPeek ( int futureTicks )
		{
			IQueuedTask actualTopOfQueue;
			IQueuedTask expectedTopOfQueue = ExpectedTopOfQueueTask;

			for ( int i = 0; i <= futureTicks; i++ )
			{
				PostgreSqlTaskQueueInfo taskQueue = CreateTaskQueue( () => mDataSource
					.LastPostedAt
					.AddTicks( futureTicks ) );

				actualTopOfQueue = await taskQueue.PeekAsync();

				Assert.NotNull( actualTopOfQueue );
				Assert.AreEqual( expectedTopOfQueue.Id,
					actualTopOfQueue.Id );
			}
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanComputeQueueMetrics ( int futureTicks )
		{
			for ( int i = 0; i <= futureTicks; i++ )
			{
				PostgreSqlTaskQueueInfo taskQueue = CreateTaskQueue( () => mDataSource
					.LastPostedAt
					.AddTicks( futureTicks ) );

				TaskQueueMetrics metrics = await taskQueue
					.ComputeMetricsAsync();

				Assert.NotNull( metrics );
				Assert.AreEqual( mDataSource.NumUnProcessedTasks, metrics.TotalUnprocessed );
				Assert.AreEqual( mDataSource.NumErroredTasks, metrics.TotalErrored );
				Assert.AreEqual( mDataSource.NumFaultedTasks, metrics.TotalFaulted );
				Assert.AreEqual( mDataSource.NumFatalTasks, metrics.TotalFataled );
				Assert.AreEqual( mDataSource.NumProcessedTasks, metrics.TotalProcessed );
			}
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueue ( Func<DateTimeOffset> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TaskQueueTimestampProvider( currentTimeProvider ) );
		}

		private IQueuedTask ExpectedTopOfQueueTask
			=> mDataSource.SeededTasks
				.OrderByDescending( t => t.Priority )
				.OrderBy( t => t.PostedAtTs )
				.OrderBy( t => t.LockHandleId )
				.FirstOrDefault();

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
