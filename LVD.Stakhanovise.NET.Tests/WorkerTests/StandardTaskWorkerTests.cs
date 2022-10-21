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
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.WorkerTests
{
	[TestFixture]
	public class StandardTaskWorkerTests
	{
		private Type [] mPayloadTypes = new Type []
		{
			typeof(ErroredTaskPayload),
			typeof(ImplicitSuccessfulTaskPayload),
			typeof(SuccessfulTaskPayload),
			typeof(ThrowsExceptionTaskPayload),
			typeof(AnotherSampleTaskPayload)
		};

		[Test]
		public async Task Test_CanStartStop()
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();
			Mock<ITaskBuffer> bufferMock =
				new Mock<ITaskBuffer>( MockBehavior.Loose );
			Mock<ITaskExecutorRegistry> executorRegistryMock =
				new Mock<ITaskExecutorRegistry>( MockBehavior.Loose );
			Mock<IExecutionPerformanceMonitor> executionPerformanceMonitorMock =
				new Mock<IExecutionPerformanceMonitor>( MockBehavior.Loose );
			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>( MockBehavior.Loose );
			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>( MockBehavior.Loose );

			using ( StandardTaskWorker worker = CreateWorker( processingOpts,
				bufferMock.Object,
				executorRegistryMock.Object,
				executionPerformanceMonitorMock.Object,
				producerMock.Object,
				resultQueueMock.Object ) )
			{
				await worker.StartAsync();
				Assert.IsTrue( worker.IsRunning );

				await worker.StopAync();
				Assert.IsFalse( worker.IsRunning );
			}
		}

		private StandardTaskWorker CreateWorker( TaskProcessingOptions processingOpts,
			ITaskBuffer buffer,
			ITaskExecutorRegistry executorRegistry,
			IExecutionPerformanceMonitor executionPerformanceMonitor,
			ITaskQueueProducer taskQueueProducer,
			ITaskResultQueue resultQueue )
		{
			IStakhanoviseLoggingProvider loggingProvider = StakhanoviseLogManager
				.Provider;

			ITaskExecutionMetricsProvider metricsProvider =
				new StandardTaskExecutionMetricsProvider();

			ITaskExecutorBufferHandlerFactory bufferHandlerFactory =
				new StandardTaskExecutorBufferHandlerFactory( buffer,
					metricsProvider,
					loggingProvider );

			ITaskExecutorResolver taskExecutorResolver =
				new StandardTaskExecutorResolver( executorRegistry,
					loggingProvider.CreateLogger<StandardTaskExecutorResolver>() );

			ITaskExecutionRetryCalculator executionRetryCalculator =
				new StandardTaskExecutionRetryCalculator( processingOpts,
					loggingProvider.CreateLogger<StandardTaskExecutionRetryCalculator>() );

			ITaskProcessor taskProcessor =
				new StandardTaskProcessor( processingOpts,
					taskExecutorResolver,
					executionRetryCalculator,
					loggingProvider.CreateLogger<StandardTaskProcessor>() );

			ITaskExecutionResultProcessor resultProcessor =
				new StandardTaskExecutionResultProcessor( resultQueue,
					taskQueueProducer,
					executionPerformanceMonitor,
					loggingProvider.CreateLogger<StandardTaskExecutionResultProcessor>() );

			return new StandardTaskWorker(
				taskProcessor,
				resultProcessor,
				bufferHandlerFactory,
				metricsProvider,
				loggingProvider.CreateLogger<StandardTaskWorker>()
			);
		}

		[Test]
		[TestCase( 1, 1, 1 )]
		[TestCase( 10, 1, 1 )]
		[TestCase( 1, 5, 1 )]
		[TestCase( 5, 5, 5 )]
		[TestCase( 100, 10, 10 )]
		public async Task Test_CanWork( int bufferCapacity, int workerCount, int numberOfTasks )
		{
			ConcurrentBag<IQueuedTaskResult> processedTaskTokensResults =
				new ConcurrentBag<IQueuedTaskResult>();

			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			Mock<IExecutionPerformanceMonitor> executionPerformanceMonitorMock =
				new Mock<IExecutionPerformanceMonitor>();

			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>( MockBehavior.Loose );

			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>( MockBehavior.Loose );

			List<StandardTaskWorker> workers
				= new List<StandardTaskWorker>();

			executionPerformanceMonitorMock.Setup( m => m.ReportExecutionTimeAsync(
				It.IsIn( mPayloadTypes.Select( t => t.FullName ).ToArray() ),
				It.IsAny<long>(),
				It.IsAny<int>() ) );

			resultQueueMock.Setup( rq => rq.PostResultAsync( It.IsAny<IQueuedTaskResult>() ) )
				.Callback<IQueuedTaskResult>( t => processedTaskTokensResults.Add( t ) );

			//TODO: must also test that, for failed tasks that can be re-posted, 
			//	the tasks is actually reposted
			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			{
				TestBufferProducer producer =
					new TestBufferProducer( taskBuffer, mPayloadTypes );

				for ( int i = 0; i < workerCount; i++ )
					workers.Add( CreateWorker( processingOpts,
						taskBuffer,
						CreateTaskExecutorRegistry(),
						executionPerformanceMonitorMock.Object,
						producerMock.Object,
						resultQueueMock.Object ) );

				foreach ( StandardTaskWorker w in workers )
					await w.StartAsync();

				await producer.ProduceTasksAsync( numberOfTasks );

				while ( !taskBuffer.IsCompleted )
					await Task.Delay( 50 );

				await Task.Delay( 250 );
				foreach ( StandardTaskWorker w in workers )
					await w.StopAync();

				foreach ( StandardTaskWorker w in workers )
					w.Dispose();

				producer.AssertMatchesProcessedTasks( processedTaskTokensResults );
				executionPerformanceMonitorMock.VerifyAll();
			}
		}

		private ITaskExecutorRegistry CreateTaskExecutorRegistry()
		{
			ITaskExecutorRegistry registry = new StandardTaskExecutorRegistry( GetDependencyResolver() );
			registry.ScanAssemblies( CurrentAssembly );
			return registry;
		}

		private IDependencyResolver GetDependencyResolver()
		{
			IDependencyResolver dependencyResolver =
				new StandardDependencyResolver();

			dependencyResolver.Load( TestDependencyRegistrations
				.GetAll() );

			return dependencyResolver;
		}

		private Assembly CurrentAssembly
			=> GetType().Assembly;
	}
}
