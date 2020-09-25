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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using Moq;
using Ninject;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class DefaultTaskWorkerTests
	{
		private IKernel mKernel;

		public DefaultTaskWorkerTests ()
		{
			mKernel = new StandardKernel( new NinjectTasksTestModule() );
		}

		[Test]
		public async Task Test_CanStartStop ()
		{
			Mock<ITaskBuffer> bufferMock =
				new Mock<ITaskBuffer>( MockBehavior.Loose );
			Mock<ITaskExecutorRegistry> executorRegistryMock =
				new Mock<ITaskExecutorRegistry>( MockBehavior.Loose );
			Mock<IKernel> kernelMock =
				new Mock<IKernel>( MockBehavior.Loose );
			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>( MockBehavior.Loose );

			using ( StandardTaskWorker worker = new StandardTaskWorker( bufferMock.Object,
				executorRegistryMock.Object,
				resultQueueMock.Object ) )
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
			List<QueuedTask> producedTasks;
			ITaskExecutorRegistry executorRegistry
				= new StandardTaskExecutorRegistry( type => mKernel.TryGet( type ) );
			List<StandardTaskWorker> workers
				= new List<StandardTaskWorker>();

			executorRegistry.ScanAssemblies( GetType().Assembly );

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( InMemoryMockTaskResultQueue taskResultQueue = new InMemoryMockTaskResultQueue() )
			{
				for ( int i = 0; i < workerCount; i++ )
					workers.Add( new StandardTaskWorker( taskBuffer,
						executorRegistry,
						taskResultQueue ) );

				foreach ( StandardTaskWorker w in workers )
					await w.StartAsync();

				producedTasks = await ProduceBuffer( taskBuffer, numberOfTasks );
				while ( !taskBuffer.IsCompleted )
					await Task.Delay( 25 );

				foreach ( StandardTaskWorker w in workers )
					await w.StopAync();

				Assert.AreEqual( producedTasks.Count,
					taskResultQueue.TaskResults.Count );

				foreach ( QueuedTask produced in producedTasks )
					Assert.IsTrue( taskResultQueue.TaskResults.ContainsKey( produced ) );

				foreach ( StandardTaskWorker w in workers )
					w.Dispose();
			}
		}

		private Task<List<QueuedTask>> ProduceBuffer ( ITaskBuffer taskBuffer, int numberOfTasks )
		{
			List<QueuedTask> producedTasks
				= new List<QueuedTask>();

			TaskCompletionSource<List<QueuedTask>> producedTasksCompletionSource
				= new TaskCompletionSource<List<QueuedTask>>();

			ManualResetEvent bufferSpaceAvailableWaitHandle =
				new ManualResetEvent( false );

			Queue<Type> taskPayloadTypes = new Queue<Type>( new Type[]
			{
				typeof(ErroredTaskPayload),
				typeof(ImplicitSuccessfulTaskPayload),
				typeof(SuccessfulTaskPayload),
				typeof(ThrowsExceptionTaskPayload)
			} );

			Task.Run( () =>
			{
				QueuedTask newTask;
				Type currentPayloadType;

				EventHandler onBufferElementRemoved
					= ( s, e ) => bufferSpaceAvailableWaitHandle.Set();

				taskBuffer.QueuedTaskRetrieved
					+= onBufferElementRemoved;

				while ( taskPayloadTypes.TryDequeue( out currentPayloadType ) )
				{
					for ( int i = 0; i < numberOfTasks; i++ )
					{
						newTask = new QueuedTask( Guid.NewGuid() )
						{
							Payload = Activator.CreateInstance( currentPayloadType ),
							Status = QueuedTaskStatus.Unprocessed,
							Type = currentPayloadType.FullName
						};

						producedTasks.Add( newTask );

						while ( !taskBuffer.TryAddNewTask( newTask ) )
						{
							bufferSpaceAvailableWaitHandle.WaitOne();
							bufferSpaceAvailableWaitHandle.Reset();
						}
					}
				}

				taskBuffer.CompleteAdding();

				producedTasksCompletionSource
					.TrySetResult( producedTasks );

				taskBuffer.QueuedTaskRetrieved
					-= onBufferElementRemoved;
			} );

			return producedTasksCompletionSource.Task;
		}
	}
}
