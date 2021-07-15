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
using Bogus;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Tests.Support;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: make test scenarios with and without new task notification updates
	[TestFixture]
	public class StandardTaskPollerTests
	{
		private const int MinMillisecondDelayAmount = 250;

		private const int MaxMillisecondDelayAmount = 1250;

		private const int MinTaskBufferCapacity = 1;

		private const int MaxTaskBufferCapacity = 100;

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanStartStop()
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			using ( StandardTaskBuffer taskBuffer = CreateStandardTaskBufferWithRandomCapacity() )
			using ( MockTaskQueueConsumer taskQueue = CreateMockTaskQueuConsumer( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer ) )
			{
				await poller.StartAsync();

				Assert.IsTrue( poller.IsStarted );
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await poller.StopAync();

				Assert.IsFalse( poller.IsStarted );
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		private StandardTaskBuffer CreateStandardTaskBufferWithRandomCapacity()
		{
			return new StandardTaskBuffer( GenerateTaskBufferCapacity() );
		}

		private int GenerateTaskBufferCapacity()
		{
			Faker faker = new Faker();
			return faker.Random.Int( MinTaskBufferCapacity, MaxTaskBufferCapacity );
		}

		private MockTaskQueueConsumer CreateMockTaskQueuConsumer( int numberOfTasksToGenerate )
		{
			return new MockTaskQueueConsumer( numberOfTasksToGenerate,
				new UtcNowTimestampProvider() );
		}

		[Test]
		[TestCase( 150, 10 )]
		[TestCase( 1, 1 )]
		[TestCase( 1, 150 )]
		[TestCase( 10, 150 )]
		[TestCase( 150, 150 )]
		[TestCase( 10, 1 )]
		[Repeat( 10 )]
		public async Task Test_CanPoll( int bufferCapacity, int numberOfTasks )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueue = CreateMockTaskQueuConsumer( numberOfTasks ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer ) )
			{
				TestBufferConsumer consumer =
					new TestBufferConsumer( taskBuffer );

				await poller.StartAsync();
				
				consumer.ConsumeBuffer();
				taskQueue.WaitForQueueToBeDepleted();
				
				await poller.StopAync();
				consumer.WaitForBufferToBeConsumed();

				Assert.IsFalse( taskBuffer.HasTasks );
				Assert.IsTrue( taskBuffer.IsCompleted );

				consumer.AssertMatchesProducedTasks( taskQueue
					.DequeuedTasksHistory );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public async Task Test_TryPoll_NoTaskInQueue_ThenShutdown( int bufferCapacity )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueue = CreateMockTaskQueuConsumer( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer ) )
			{
				await poller.StartAsync();
				await Task.Delay( GenerateMilliseconDelayAmount() );
				await poller.StopAync();

				Assert.AreEqual( 1, taskQueue.DequeueCallCount );
				Assert.IsTrue( taskBuffer.IsEmpty );
			}
		}

		private int GenerateMilliseconDelayAmount()
		{
			Faker faker = new Faker();
			return faker.Random.Int( MinMillisecondDelayAmount, MaxMillisecondDelayAmount );
		}

		[Test]
		[TestCase( 150, 10 )]
		[TestCase( 1, 1 )]
		[TestCase( 1, 150 )]
		[TestCase( 10, 150 )]
		[TestCase( 150, 150 )]
		[TestCase( 10, 1 )]
		[Repeat( 10 )]
		public async Task Test_TryPoll_FullBuffer_ThenShutdown( int bufferCapacity, int numberOfTasks )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			using ( StandardTaskBuffer taskBuffer = new StandardTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueue = CreateMockTaskQueuConsumer( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueue,
				taskBuffer ) )
			{
				TestBufferFiller filler =
					new TestBufferFiller( taskBuffer );

				filler.FillBuffer();

				await poller.StartAsync();
				await Task.Delay( GenerateMilliseconDelayAmount() );
				await poller.StopAync();

				Assert.AreEqual( 0, taskQueue.DequeueCallCount );
				Assert.IsTrue( taskBuffer.IsFull );
			}
		}
	}
}
