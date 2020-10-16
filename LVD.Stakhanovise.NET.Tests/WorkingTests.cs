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
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class WorkingTests : BaseTestWithConfiguration
	{
		private PostgreSqlTaskQueueDbOperations mOperations;

		public WorkingTests ()
		{
			mOperations = new PostgreSqlTaskQueueDbOperations( CommonConnectionString, TestOptions.DefaultMapping );
		}

		[OneTimeSetUp]
		public void FixtureSetUp ()
		{
			StakhanoviseLogManager.Provider = new ConsoleLoggingProvider( StakhanoviseLogLevel.Debug,
				writeToStdOut: true );
		}

		[SetUp]
		public async Task SetUp ()
		{
			await mOperations.ClearTaskAndResultDataAsync();
			await mOperations.ClearExecutionPerformanceTimeStatsTableAsync();
			await mOperations.ResetTimeDbTableAsync( TimingBeltTimeId );

			ResultStorage<ComputeFactorialResult>.Instance
				.Clear();
		}

		[TearDown]
		public async Task TearDown ()
		{
			await mOperations.ClearTaskAndResultDataAsync();
			await mOperations.ClearExecutionPerformanceTimeStatsTableAsync();
			await mOperations.ResetTimeDbTableAsync( TimingBeltTimeId );

			ResultStorage<ComputeFactorialResult>.Instance
				.Clear();
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[Repeat( 10 )]
		public async Task Test_DoWork_NoWorkToBeDone ( int workerCount )
		{
			Faker faker =
				new Faker();
			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			await taskEngine.StartAsync();
			await Task.Delay( faker.Random.Int( 500, 5000 ) );
			await taskEngine.StopAync();

			//TODO: thins that need to be tested:
			//	- abstract time is not ticked forward
			//	- no changes in task and result queues
			//	- no changes in task execution stats
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		public async Task Test_DoWork_TasksWithNoErrors ( int workerCount )
		{
			CountdownEvent doneEvent =
				new CountdownEvent( 20 );

			ITaskQueueProducer producer =
				CreateTaskQueueProducer();

			ITaskEngine taskEngine =
				CreateTaskEngine( workerCount );

			ResultStorage<ComputeFactorialResult>.Instance
				.ItemAdded += ( s, e ) => doneEvent.Signal();

			await taskEngine.StartAsync();

			for ( int i = 1; i <= 20; i++ )
				await producer.EnqueueAsync( new ComputeFactorial( i ),
					source: nameof( Test_DoWork_TasksWithNoErrors ),
					priority: 0 );

			doneEvent.Wait();
			await taskEngine.StopAync();

			AssertNoMoreTasksLeft();
			AssertAllTasksCompletedSuccessfully();
			AssertCorrectExecutionTimeStats();
			AssertCorrectCurrentAbstractTime();
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		public async Task Test_DoWork_TasksWithErrors ( int workerCount )
		{

		}

		private void AssertNoMoreTasksLeft ()
		{

		}

		private void AssertAllTasksCompletedSuccessfully ()
		{

		}

		private void AssertCorrectExecutionTimeStats ()
		{

		}

		private void AssertCorrectCurrentAbstractTime ()
		{

		}

		private ITaskEngine CreateTaskEngine ( int workerCount )
		{
			ITaskEngine engine = new StandardTaskEngine( GetTaskEngineOptions( workerCount ),
				GetProduerAndResultOptions(),
				GetConsumerOptions(),
				CreateTaskExecutorRegistry(),
				CreateTimingBelt(),
				CreateExecutionPeformanceMonitorWriter() );

			engine.ScanAssemblies( GetType().Assembly );
			return engine;
		}

		private ITaskExecutorRegistry CreateTaskExecutorRegistry ()
		{
			return new StandardTaskExecutorRegistry( new StandardDependencyResolver() );
		}

		private IExecutionPerformanceMonitorWriter CreateExecutionPeformanceMonitorWriter ()
		{
			return new PostgreSqlExecutionPerformanceMonitorWriter( TestOptions
				.GetDefaultPostgreSqlExecutionPerformanceMonitorWriterOptions( CommonConnectionString ) );
		}

		private ITaskQueueTimingBelt CreateTimingBelt ()
		{
			return new PostgreSqlTaskQueueTimingBelt( TestOptions
				.GetDefaultPostgreSqlTaskQueueTimingBeltOptions(
					TimingBeltTimeId,
					CommonConnectionString ) );
		}

		private ITaskQueueProducer CreateTaskQueueProducer ()
		{
			return new PostgreSqlTaskQueueProducer( TestOptions.GetDefaultTaskQueueProducerAndResultOptions( CommonConnectionString ),
				timeProvider: CreateAbstractTimeProvider() );
		}

		private ITaskQueueAbstractTimeProvider CreateAbstractTimeProvider ()
		{
			return new PostgreSqlTaskQueueAbstractTimeProvider( TestOptions
				.GetDefaultPostgreSqlTaskQueueAbstractTimeProviderOptions(
					TimingBeltTimeId,
					CommonConnectionString ) );
		}

		private TaskEngineOptions GetTaskEngineOptions ( int workerCount )
		{
			return new TaskEngineOptions( workerCount,
				perfMonOptions: TestOptions.GetDefaultExecutionPerformanceMonitorOptions(),
				taskProcessingOptions: TestOptions.GetDefaultTaskProcessingOptions() );
		}

		private TaskQueueConsumerOptions GetConsumerOptions ()
		{
			return TestOptions.GetDefaultTaskQueueConsumerOptions( BaseConsumerConnectionString );
		}

		private TaskQueueOptions GetProduerAndResultOptions ()
		{
			return TestOptions.GetDefaultTaskQueueProducerAndResultOptions( CommonConnectionString );
		}

		private string BaseConsumerConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );

		private string CommonConnectionString
			=> GetConnectionString( "testDbConnectionString" );

		private Guid TimingBeltTimeId
			=> Guid.Parse( GetAppSetting( "timeId" ) );
	}
}
