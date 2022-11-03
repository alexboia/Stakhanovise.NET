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
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.ResultsTests
{
	[TestFixture]
	public class PostgreSqlTaskResultQueueTests : BaseDbTests
	{
		private TaskQueueDataSource mDataSource;

		private TaskQueueOptions mResultQueueOptions;

		public PostgreSqlTaskResultQueueTests()
		{
			mResultQueueOptions = TestOptions
				.GetDefaultTaskResultQueueOptions( ConnectionString );

			mDataSource = new TaskQueueDataSource( mResultQueueOptions.ConnectionString,
				mResultQueueOptions.Mapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[SetUp]
		public async Task TestSetUp()
		{
			await mDataSource.SeedData();
			await Task.Delay( 100 );
		}

		[TearDown]
		public async Task TestTearDown()
		{
			await mDataSource.ClearData();
			await Task.Delay( 100 );
		}

		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 100 )]
		[TestCase( 1, 1000 )]
		[TestCase( 1, 10000 )]

		[TestCase( 2, 0 )]
		[TestCase( 2, 100 )]
		[TestCase( 2, 1000 )]
		[TestCase( 2, 10000 )]

		[TestCase( 5, 0 )]
		[TestCase( 5, 100 )]
		[TestCase( 5, 1000 )]

		[TestCase( 10, 0 )]
		[TestCase( 10, 100 )]
		[TestCase( 10, 1000 )]
		public async Task Test_CanStartStop( int repeatCycles, int timeBetweenStartStopCalls )
		{
			using ( PostgreSqlTaskResultQueue rq = CreateResultQueue() )
			{
				for ( int i = 0; i < repeatCycles; i++ )
				{
					await rq.StartAsync();
					Assert.IsTrue( rq.IsRunning );

					if ( timeBetweenStartStopCalls > 0 )
						await Task.Delay( timeBetweenStartStopCalls );

					await rq.StopAsync();
					Assert.IsFalse( rq.IsRunning );
				}
			}
		}

		[Test]
		[Repeat( 15 )]
		public async Task Test_CanPostResult_SuccessfulResults_SerialCalls()
		{
			await RunPostResultTests( CreateSuccessfulResult );
		}

		private TaskExecutionResult CreateSuccessfulResult()
		{
			Faker faker = new Faker();
			return new TaskExecutionResult( TaskExecutionResultInfo.Successful(),
				duration: faker.Date.Timespan(),
				retryAt: DateTimeOffset.UtcNow,
				faultErrorThresholdCount: mDataSource.QueueFaultErrorThresholdCount );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanPostResult_FailedResults_SerialCalls()
		{
			await RunPostResultTests( CreateFailedResult );
		}

		private TaskExecutionResult CreateFailedResult()
		{
			Faker faker = new Faker();

			TaskExecutionResultInfo resultInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( faker.System.Exception() ),
					isRecoverable: faker.Random.Bool() );

			return new TaskExecutionResult( resultInfo,
				duration: TimeSpan.Zero,
				retryAt: DateTimeOffset.UtcNow,
				faultErrorThresholdCount: mDataSource.QueueFaultErrorThresholdCount );
		}

		private async Task RunPostResultTests( Func<TaskExecutionResult> executionResultFactory )
		{
			PostResultsToQueueTestsRunner runner =
				CreatePostResultsToQueueTestsRunner( executionResultFactory );

			using ( PostgreSqlTaskResultQueue resultQueue = CreateResultQueue() )
				await runner.RunTestsAsync( resultQueue );

			await CheckExpectedResultsAsync( runner.Results );
		}

		private PostResultsToQueueTestsRunner CreatePostResultsToQueueTestsRunner( Func<TaskExecutionResult> executionResultFactory )
		{
			return new PostResultsToQueueTestsRunner( mDataSource,
				executionResultFactory );
		}

		private async Task CheckExpectedResultsAsync( List<IQueuedTaskResult> expectedResults )
		{
			foreach ( IQueuedTaskResult expectedResult in expectedResults )
			{
				QueuedTaskResult dbResult = await mDataSource
					.GetQueuedTaskResultFromDbByIdAsync( expectedResult.Id );

				AssertQueuedTaskResultMatchesExpectedResult
					.For( expectedResult )
					.Check( dbResult );
			}
		}

		private PostgreSqlTaskResultQueue CreateResultQueue()
		{
			return new PostgreSqlTaskResultQueue( mResultQueueOptions,
				new StandardTaskResultQueueMetricsProvider() );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
