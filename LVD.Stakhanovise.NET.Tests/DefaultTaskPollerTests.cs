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

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class DefaultTaskPollerTests
	{
		[Test]
		public async Task Test_CanStartStop ()
		{
			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( 100 ) )
			using ( DequeueOnlyMockTaskQueue taskQueue = new DequeueOnlyMockTaskQueue( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( taskQueue, taskBuffer ) )
			{
				await poller.StartAsync();

				Assert.IsTrue( poller.IsRunning );
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await poller.StopAync();

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
			List<QueuedTask> producedTasks;
			List<QueuedTask> consumedTasks;
			Task<List<QueuedTask>> consumedTasksReadyHandle;

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( DequeueOnlyMockTaskQueue taskQueue = new DequeueOnlyMockTaskQueue( numberOfTasks ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( taskQueue, taskBuffer ) )
			{
				await poller.StartAsync();

				consumedTasksReadyHandle = ConsumeBuffer( taskBuffer );

				await taskQueue.QueueDepletedHandle;
				await poller.StopAync();

				producedTasks = taskQueue.DequeuedTasksHistory;
				consumedTasks = await consumedTasksReadyHandle;

				Assert.IsFalse( taskBuffer.HasTasks );
				Assert.IsTrue( taskBuffer.IsCompleted );

				Assert.AreEqual( producedTasks.Count, consumedTasks.Count );

				foreach ( QueuedTask pt in producedTasks )
					Assert.AreEqual( 1, consumedTasks.Count( ct => ct.Id == pt.Id ) );
			}
		}

		private Task<List<QueuedTask>> ConsumeBuffer ( ITaskBuffer taskBuffer )
		{
			List<QueuedTask> consumedTasks
				= new List<QueuedTask>();

			TaskCompletionSource<List<QueuedTask>> consumedTasksCompletionSource
				= new TaskCompletionSource<List<QueuedTask>>();

			Task.Run( () =>
			{
				while ( !taskBuffer.IsCompleted )
				{
					QueuedTask queuedTask = taskBuffer.TryGetNextTask();
					if ( queuedTask != null )
						consumedTasks.Add( queuedTask );
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
