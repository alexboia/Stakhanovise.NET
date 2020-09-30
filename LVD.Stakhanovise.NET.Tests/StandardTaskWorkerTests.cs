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

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StandardTaskWorkerTests
	{
		private Type[] mPayloadTypes = new Type[]
		{
			typeof(ErroredTaskPayload),
			typeof(ImplicitSuccessfulTaskPayload),
			typeof(SuccessfulTaskPayload),
			typeof(ThrowsExceptionTaskPayload),
			typeof(AnotherSampleTaskPayload)
		};

		[Test]
		public async Task Test_CanStartStop ()
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();
			Mock<ITaskBuffer> bufferMock =
				new Mock<ITaskBuffer>( MockBehavior.Loose );
			Mock<ITaskExecutorRegistry> executorRegistryMock =
				new Mock<ITaskExecutorRegistry>( MockBehavior.Loose );
			Mock<IExecutionPerformanceMonitor> executionPerformanceMonitorMock =
				new Mock<IExecutionPerformanceMonitor>( MockBehavior.Loose );

			using ( InMemoryTaskQueueTimingBelt timingBelt = new InMemoryTaskQueueTimingBelt( initialWallclockTimeCost: 1000 ) )
			using ( StandardTaskWorker worker = new StandardTaskWorker( processingOpts,
				bufferMock.Object,
				executorRegistryMock.Object,
				executionPerformanceMonitorMock.Object,
				timingBelt ) )
			{
				await worker.StartAsync();
				Assert.IsTrue( worker.IsRunning );

				await worker.StopAync();
				Assert.IsFalse( worker.IsRunning );
			}
		}

		[Test]
		[TestCase( 1, 1, 1 )]
		[TestCase( 10, 1, 1 )]
		[TestCase( 1, 5, 1 )]
		[TestCase( 5, 5, 5 )]
		[TestCase( 100, 10, 10 )]
		public async Task Test_CanWork ( int bufferCapacity, int workerCount, int numberOfTasks )
		{
			ConcurrentBag<IQueuedTaskToken> processedTaskTokens =
				new ConcurrentBag<IQueuedTaskToken>();

			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			Mock<IExecutionPerformanceMonitor> executionPerformanceMonitorMock =
				new Mock<IExecutionPerformanceMonitor>();

			List<StandardTaskWorker> workers
				= new List<StandardTaskWorker>();

			Action<object, TokenReleasedEventArgs> handleTokenReleased =
				( s, e ) => processedTaskTokens.Add( ( IQueuedTaskToken )s );

			executionPerformanceMonitorMock.Setup( m => m.ReportExecutionTime(
				It.IsIn( mPayloadTypes.Select( t => t.FullName ).ToArray() ),
				It.IsAny<long>() ) );

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( InMemoryTaskQueueTimingBelt timingBelt = new InMemoryTaskQueueTimingBelt( initialWallclockTimeCost: 1000 ) )
			{
				TestBufferProducer producer = 
					new TestBufferProducer( taskBuffer, mPayloadTypes );
				
				for ( int i = 0; i < workerCount; i++ )
					workers.Add( new StandardTaskWorker( processingOpts,
						taskBuffer,
						CreateTaskExecutorRegistry(),
						executionPerformanceMonitorMock.Object,
						timingBelt ) );

				await timingBelt.StartAsync();
				foreach ( StandardTaskWorker w in workers )
					await w.StartAsync();

				await producer.ProduceTasksAsync( numberOfTasks,
					handleTokenReleased );

				while ( !taskBuffer.IsCompleted )
					await Task.Delay( 25 );

				foreach ( StandardTaskWorker w in workers )
					await w.StopAync();

				await timingBelt.StopAsync();
				foreach ( StandardTaskWorker w in workers )
					w.Dispose();

				producer.AssertMatchesProcessedTasks( processedTaskTokens );
				executionPerformanceMonitorMock.VerifyAll();
			}
		}

		private ITaskExecutorRegistry CreateTaskExecutorRegistry ()
		{
			ITaskExecutorRegistry registry = new StandardTaskExecutorRegistry( GetDependencyResolver() );
			registry.ScanAssemblies( CurrentAssembly );
			return registry;
		}

		private IDependencyResolver GetDependencyResolver ()
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
