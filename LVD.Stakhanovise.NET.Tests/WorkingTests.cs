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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Executors.Working;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Payloads.Working;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class WorkingTests : BaseDbTests
	{
		private string mTestProcessId;

		private ITimestampProvider mTimestampProvider;

		private TaskQueueOptions mProducerAndResultOptions;

		private TaskProcessingOptions mTaskProcessingOptions;

		private TaskQueueConsumerOptions mTaskQueueConsumerOptions;

		private PostgreSqlExecutionPerformanceMonitorWriterOptions mPostgreSqlExecutionPerformanceMonitorWriterOptions;

		private PostgreSqlTaskQueueDbOperations mOperations;

		public WorkingTests()
		{
			mTestProcessId = Guid.NewGuid()
				.ToString();

			mTimestampProvider = new UtcNowTimestampProvider();

			mTaskProcessingOptions = TestOptions
				.GetDefaultTaskProcessingOptions();

			mProducerAndResultOptions = TestOptions
				.GetDefaultTaskQueueProducerAndResultOptions( CommonConnectionString );

			mTaskQueueConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( BaseConsumerConnectionString );

			mPostgreSqlExecutionPerformanceMonitorWriterOptions = TestOptions
				.GetDefaultPostgreSqlExecutionPerformanceMonitorWriterOptions( CommonConnectionString );

			mOperations = new PostgreSqlTaskQueueDbOperations( CommonConnectionString,
				TestOptions.DefaultMapping );
		}

		[SetUp]
		public async Task SetUp()
		{
			await mOperations.ClearTaskAndResultDataAsync();
			await mOperations.ClearExecutionPerformanceTimeStatsTableAsync();

			TestExecutorEventBus<ComputeFactorial>.Instance
				.Reset();
			TestExecutorEventBus<AlwaysFailingTask>.Instance
				.Reset();
			TestExecutorEventBus<FailsNTimesBeforeSucceeding>.Instance
				.Reset();
			FailsNTimesBeforeSucceedingExecutor
				.ResetFailCounts();
		}

		[TearDown]
		public async Task TearDown()
		{
			await mOperations.ClearTaskAndResultDataAsync();
			await mOperations.ClearExecutionPerformanceTimeStatsTableAsync();

			TestExecutorEventBus<ComputeFactorial>.Instance
				.Reset();
			TestExecutorEventBus<AlwaysFailingTask>.Instance
				.Reset();
			TestExecutorEventBus<FailsNTimesBeforeSucceeding>.Instance
				.Reset();
			FailsNTimesBeforeSucceedingExecutor
				.ResetFailCounts();
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[Repeat( 10 )]
		public async Task Test_DoWork_NoWorkToBeDone( int workerCount )
		{
			Faker faker =
				new Faker();
			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			Dictionary<string, long> cyclesCountBefore =
				await GetAllExecutionCyclesCounts();

			await taskEngine.StartAsync();
			await Task.Delay( faker.Random.Int( 500, 5000 ) );
			await taskEngine.StopAync();

			Dictionary<string, long> cyclesCountAfter =
				await GetAllExecutionCyclesCounts();

			await AssertTotalTasks( expectedTotal: 0 );
			await AssertTotalTaskResults( expectedTotal: 0 );

			CollectionAssert.AreEqual( cyclesCountBefore,
				cyclesCountAfter );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		public async Task Test_DoWork_TasksWithNoErrors( int workerCount )
		{
			CountdownEvent doneEvent =
				new CountdownEvent( 20 );

			ITaskQueueProducer producer =
				CreateTaskQueueProducer();

			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			TestExecutorEventBus<ComputeFactorial>.Instance
				.ExecutorCompleted += ( s, e ) => doneEvent.Signal();

			await taskEngine.StartAsync();

			for ( int i = 1; i <= 20; i++ )
				await producer.EnqueueAsync( new ComputeFactorial( i ),
					source: nameof( Test_DoWork_TasksWithNoErrors ),
					priority: 0 );

			doneEvent.Wait();
			await taskEngine.StopAync();

			await AssertTotalTasks( expectedTotal: 0 );

			await AssertAllTasksCompletedWithStatus(
				expectedTaskResultCount: 20,
				expectedStatus: QueuedTaskStatus.Processed,
				expectedErrorCount: 0 );

			await AssertCorrectExecutionCyclesCount<ComputeFactorial>( expectedCount:
				20 );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[Repeat( 5 )]
		public async Task Test_DoWork_TasksWithErrors_CompletesSuccessfullyAfterFailures( int workerCount )
		{
			ITaskQueueProducer producer =
				CreateTaskQueueProducer();

			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			int numberForErrorsBeforeSucceeding = taskEngine.Options
				.TaskProcessingOptions
				.FaultErrorThresholdCount - 1;

			CountdownEvent doneEvent =
				new CountdownEvent( 20 * ( numberForErrorsBeforeSucceeding + 1 ) );

			TestExecutorEventBus<FailsNTimesBeforeSucceeding>.Instance
				.ExecutorCompleted += ( s, e ) => doneEvent.Signal();

			await taskEngine.StartAsync();

			for ( int i = 1; i <= 20; i++ )
				await producer.EnqueueAsync( new FailsNTimesBeforeSucceeding( Guid.NewGuid(),
						numberForErrorsBeforeSucceeding ),
					source: nameof( Test_DoWork_TasksWithErrors_CompletesSuccessfullyAfterFailures ),
					priority: 0 );

			doneEvent.Wait();
			await taskEngine.StopAync();

			await AssertTotalTasks( expectedTotal: 0 );

			await AssertAllTasksCompletedWithStatus( expectedTaskResultCount: 20,
				expectedStatus: QueuedTaskStatus.Processed,
				expectedErrorCount: numberForErrorsBeforeSucceeding );

			await AssertCorrectExecutionCyclesCount<FailsNTimesBeforeSucceeding>( expectedCount:
				20 * ( numberForErrorsBeforeSucceeding + 1 ) );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[Repeat( 5 )]
		public async Task Test_DoWork_TasksWithErrors_UntilFataled( int workerCount )
		{
			ITaskQueueProducer producer =
				CreateTaskQueueProducer();

			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			int expectedNumberOfErrors = taskEngine.Options
				.TaskProcessingOptions
				.FaultErrorThresholdCount + 2;

			CountdownEvent doneEvent =
				new CountdownEvent( 20 * expectedNumberOfErrors );

			TestExecutorEventBus<AlwaysFailingTask>.Instance
				.ExecutorCompleted += ( s, e ) => doneEvent.Signal();

			await taskEngine.StartAsync();

			for ( int i = 1; i <= 20; i++ )
				await producer.EnqueueAsync( new AlwaysFailingTask(),
					source: nameof( Test_DoWork_TasksWithErrors_UntilFataled ),
					priority: 0 );

			doneEvent.Wait();
			await taskEngine.StopAync();

			await AssertTotalTasks( expectedTotal: 0 );

			await AssertAllTasksCompletedWithStatus(
				expectedTaskResultCount: 20,
				expectedStatus: QueuedTaskStatus.Fatal,
				expectedErrorCount: expectedNumberOfErrors );

			await AssertCorrectExecutionCyclesCount<AlwaysFailingTask>( expectedCount:
				20 * expectedNumberOfErrors );
		}

		private async Task AssertTotalTasks( long expectedTotal )
		{
			string countRemainingSql = $@"SELECT COUNT(1) 
				FROM {TestOptions.DefaultMapping.QueueTableName}";

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( CommonConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( countRemainingSql, conn ) )
			{
				long remainingCount = ( long ) await cmd.ExecuteScalarAsync();
				Assert.AreEqual( expectedTotal, remainingCount );
			}
		}

		private async Task AssertTotalTaskResults( long expectedTotal )
		{
			string countRemainingSql = $@"SELECT COUNT(1) 
				FROM {TestOptions.DefaultMapping.ResultsQueueTableName}";

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( CommonConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( countRemainingSql, conn ) )
			{
				long remainingCount = ( long ) await cmd.ExecuteScalarAsync();
				Assert.AreEqual( expectedTotal, remainingCount );
			}
		}

		private async Task AssertAllTasksCompletedWithStatus( int expectedTaskResultCount,
			QueuedTaskStatus expectedStatus,
			int expectedErrorCount )
		{
			string countCompletedSql = $@"SELECT COUNT(1) 
				FROM {TestOptions.DefaultMapping.ResultsQueueTableName}
				WHERE task_status = {( int ) expectedStatus} 
					AND task_error_count = {expectedErrorCount}";

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( CommonConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( countCompletedSql, conn ) )
			{
				long completedCount = ( long ) await cmd.ExecuteScalarAsync();
				Assert.AreEqual( expectedTaskResultCount, completedCount );
			}
		}

		private async Task AssertCorrectExecutionCyclesCount<TPayload>( long expectedCount )
		{
			string payloadType = typeof( TPayload )
				.FullName;

			string countCyclesSql = $@"SELECT et_n_execution_cycles 
				FROM {TestOptions.DefaultMapping.ExecutionTimeStatsTableName}  
				WHERE et_payload_type = '{payloadType}'
					AND et_owner_process_id = '{mTestProcessId}'";

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( CommonConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( countCyclesSql, conn ) )
			{
				long countCycles = ( long ) await cmd.ExecuteScalarAsync();
				Assert.AreEqual( expectedCount, countCycles );
			}
		}

		private async Task<Dictionary<string, long>> GetAllExecutionCyclesCounts()
		{
			Dictionary<string, long> payloadCyclesCounts =
				new Dictionary<string, long>();

			string getCyclesSql = $@"SELECT et_payload_type, et_n_execution_cycles 
				FROM {TestOptions.DefaultMapping.ExecutionTimeStatsTableName}
				WHERE et_owner_process_id = '{mTestProcessId}'";

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( CommonConnectionString ) )
			using ( NpgsqlCommand cmd = new NpgsqlCommand( getCyclesSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				while ( await rdr.ReadAsync() )
				{
					string payloadType = rdr.GetString( rdr
						.GetOrdinal( "et_payload_type" ) );
					long executionCycles = rdr.GetInt64( rdr
						.GetOrdinal( "et_n_execution_cycles" ) );

					payloadCyclesCounts.Add( payloadType,
						executionCycles );
				}
			}

			return payloadCyclesCounts;
		}

		private ITaskEngine CreateTaskEngine( int workerCount )
		{
			ITaskEngine engine = new StandardTaskEngine( GetTaskEngineOptions( workerCount ),
				mProducerAndResultOptions,
				mTaskQueueConsumerOptions,
				CreateTaskExecutorRegistry(),
				CreateExecutionPeformanceMonitorWriter(),
				mTimestampProvider,
				mTestProcessId );

			engine.ScanAssemblies( GetType().Assembly );
			return engine;
		}

		private ITaskExecutorRegistry CreateTaskExecutorRegistry()
		{
			return new StandardTaskExecutorRegistry( new StandardDependencyResolver() );
		}

		private IExecutionPerformanceMonitorWriter CreateExecutionPeformanceMonitorWriter()
		{
			return new PostgreSqlExecutionPerformanceMonitorWriter( mPostgreSqlExecutionPerformanceMonitorWriterOptions );
		}

		private ITaskQueueProducer CreateTaskQueueProducer()
		{
			return new PostgreSqlTaskQueueProducer( mProducerAndResultOptions, mTimestampProvider );
		}

		private TaskEngineOptions GetTaskEngineOptions( int workerCount )
		{
			return new TaskEngineOptions( workerCount, mTaskProcessingOptions );
		}

		private string BaseConsumerConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );

		private string CommonConnectionString
			=> GetConnectionString( "testDbConnectionString" );
	}
}
