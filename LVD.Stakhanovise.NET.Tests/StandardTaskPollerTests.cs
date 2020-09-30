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
using System.Linq;
using Moq;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Tests.Support;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StandardTaskPollerTests
	{
		[Test]
		public async Task Test_CanStartStop ()
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetTaskProcessingOptions();

			Mock<IExecutionPerformanceMonitor> perfMonMock = new
				Mock<IExecutionPerformanceMonitor>();

			perfMonMock.Setup( p => p.GetExecutionStats( It.IsAny<string>() ) )
				.Returns( new TaskExecutionStats( 100, 100, 100, 100, 100, 1 ) );

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( 100 ) )
			using ( MockTaskQueueConsumer taskQueue = new MockTaskQueueConsumer( 0 ) )
			using ( InMemoryTaskQueueTimingBelt timingBelt = new InMemoryTaskQueueTimingBelt( initialWallclockTimeCost: 1000 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer,
				perfMonMock.Object,
				timingBelt ) )
			{
				await timingBelt.StartAsync();
				await poller.StartAsync();

				Assert.IsTrue( poller.IsRunning );
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await poller.StopAync();
				await timingBelt.StopAsync();

				Assert.IsFalse( poller.IsRunning );
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		[Test]
		[TestCase( 150, 10 )]
		[TestCase( 1, 1 )]
		[TestCase( 1, 150 )]
		[TestCase( 10, 150 )]
		[TestCase( 150, 150 )]
		[TestCase( 10, 1 )]
		public async Task Test_PollingScenario ( int bufferCapacity, int numberOfTasks )
		{
			List<IQueuedTaskToken> producedTasks;
			List<IQueuedTaskToken> consumedTasks;
			Task<List<IQueuedTaskToken>> consumedTasksReadyHandle;

			TaskProcessingOptions processingOpts =
				TestOptions.GetTaskProcessingOptions();
			Mock<IExecutionPerformanceMonitor> perfMonMock = 
				new Mock<IExecutionPerformanceMonitor>();
			
			perfMonMock.Setup( p => p.GetExecutionStats( It.IsAny<string>() ) )
				.Returns( new TaskExecutionStats( 100, 100, 100, 100, 100, 1 ) );

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueue = new MockTaskQueueConsumer( numberOfTasks ) )
			using ( InMemoryTaskQueueTimingBelt timingBelt = new InMemoryTaskQueueTimingBelt( initialWallclockTimeCost: 1000 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer,
				perfMonMock.Object,
				timingBelt ) )
			{
				await timingBelt.StartAsync();
				await poller.StartAsync();

				//Poller is filling up the buffer.
				//We need to check the buffer to see whether 
				//	the poller produced the appropriate data
				consumedTasksReadyHandle = ConsumeBuffer( taskBuffer );

				await taskQueue.QueueDepletedHandle;
				await poller.StopAync();
				await timingBelt.StopAsync();

				producedTasks = taskQueue.DequeuedTasksHistory;
				consumedTasks = await consumedTasksReadyHandle;

				Assert.IsFalse( taskBuffer.HasTasks );
				Assert.IsTrue( taskBuffer.IsCompleted );

				Assert.AreEqual( producedTasks.Count, consumedTasks.Count );

				foreach ( IQueuedTaskToken pt in producedTasks )
					Assert.AreEqual( 1, consumedTasks.Count( ct => ct.QueuedTask.Id == pt.QueuedTask.Id ) );

				perfMonMock.Verify();
			}
		}

		private Task<List<IQueuedTaskToken>> ConsumeBuffer ( ITaskBuffer taskBuffer )
		{
			List<IQueuedTaskToken> consumedTasks
				= new List<IQueuedTaskToken>();

			TaskCompletionSource<List<IQueuedTaskToken>> consumedTasksCompletionSource
				= new TaskCompletionSource<List<IQueuedTaskToken>>();

			Task.Run( () =>
			{
				while ( !taskBuffer.IsCompleted )
				{
					IQueuedTaskToken queuedTaskToken = taskBuffer.TryGetNextTask();
					if ( queuedTaskToken != null )
						consumedTasks.Add( queuedTaskToken );
					else
						Task.Delay( 10 ).Wait();
				}

				consumedTasksCompletionSource
					.TrySetResult( consumedTasks );
			} );

			return consumedTasksCompletionSource.Task;
		}
	}
}
