﻿// 
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
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.PollerTests
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
		[Repeat( 5 )]
		public async Task Test_CanStartStop()
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			MockTaskQueueProducer taskQueueProducer =
				CreateMockTaskQueueProducer();

			using ( MockTaskBuffer taskBuffer = CreateMockTaskBufferWithRandomCapacity() )
			using ( MockTaskQueueConsumer taskQueueConsumer = CreateMockTaskQueuConsumer( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueueConsumer,
				taskQueueProducer,
				taskBuffer,
				new StandardTaskPollerMetricsProvider() ) )
			{
				await poller.StartAsync();

				Assert.IsTrue( poller.IsStarted );
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				await poller.StopAync();

				Assert.IsFalse( poller.IsStarted );
				Assert.IsFalse( taskQueueConsumer.IsReceivingNewTaskUpdates );
			}
		}

		private MockTaskBuffer CreateMockTaskBufferWithRandomCapacity()
		{
			return new MockTaskBuffer( GenerateTaskBufferCapacity() );
		}

		private int GenerateTaskBufferCapacity()
		{
			Faker faker = new Faker();
			return faker.Random.Int( MinTaskBufferCapacity, MaxTaskBufferCapacity );
		}

		private MockTaskQueueConsumer CreateMockTaskQueuConsumer( int numberOfTasksToGenerate )
		{
			return new MockTaskQueueConsumer( numberOfTasksToGenerate );
		}

		private MockTaskQueueProducer CreateMockTaskQueueProducer()
		{
			return new MockTaskQueueProducer( new UtcNowTimestampProvider() );
		}

		[Test]
		[TestCase( 150, 10 )]
		[TestCase( 1, 1 )]
		[TestCase( 1, 150 )]
		[TestCase( 10, 150 )]
		[TestCase( 10, 1500 )]
		[TestCase( 150, 150 )]
		[TestCase( 10, 1 )]
		[Repeat( 5 )]
		public async Task Test_CanPoll( int bufferCapacity, int numberOfTasks )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			MockTaskQueueProducer taskQueueProducer =
				CreateMockTaskQueueProducer();

			using ( MockTaskBuffer taskBuffer = new MockTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueueConsumer = CreateMockTaskQueuConsumer( numberOfTasks ) )
			using ( TestBufferConsumer testTaskBufferConsumer = new TestBufferConsumer( taskBuffer ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueueConsumer,
				taskQueueProducer,
				taskBuffer,
				new StandardTaskPollerMetricsProvider() ) )
			{
				await poller.StartAsync();

				testTaskBufferConsumer.StartConsumingBuffer();
				taskQueueConsumer.WaitForQueueToBeDepleted();

				await poller.StopAync();
				testTaskBufferConsumer.WaitForBufferToBeConsumed();

				Assert.IsFalse( taskBuffer.HasTasks );
				Assert.IsTrue( taskBuffer.IsCompleted );
				Assert.AreEqual( taskBuffer.RefusedElementCount,
					taskQueueProducer.ProducedTasksCount );

				testTaskBufferConsumer.AssertMatchesProducedTasks( taskQueueConsumer
					.DequeuedTasksHistory );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_TryPoll_NoTaskInQueue_ThenShutdown( int bufferCapacity )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			MockTaskQueueProducer taskQueueProducer =
				CreateMockTaskQueueProducer();

			using ( MockTaskBuffer taskBuffer = new MockTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueueConsumer = CreateMockTaskQueuConsumer( 0 ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueueConsumer,
				taskQueueProducer,
				taskBuffer,
				new StandardTaskPollerMetricsProvider() ) )
			{
				await poller.StartAsync();
				await Task.Delay( GenerateMilliseconDelayAmount() );
				await poller.StopAync();

				Assert.AreEqual( 1, taskQueueConsumer.DequeueCallCount );
				Assert.IsTrue( taskBuffer.IsEmpty );

				Assert.AreEqual( taskBuffer.RefusedElementCount,
					taskQueueProducer.ProducedTasksCount );
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
		[TestCase( 10, 1500 )]
		[TestCase( 150, 150 )]
		[TestCase( 10, 1 )]
		[Repeat( 5 )]
		public async Task Test_TryPoll_FullBuffer_ThenShutdown( int bufferCapacity, int numberOfTasks )
		{
			TaskProcessingOptions processingOpts =
				TestOptions.GetDefaultTaskProcessingOptions();

			MockTaskQueueProducer taskQueueProducer =
				CreateMockTaskQueueProducer();

			using ( MockTaskBuffer taskBuffer = new MockTaskBuffer( bufferCapacity ) )
			using ( MockTaskQueueConsumer taskQueueConsumer = CreateMockTaskQueuConsumer( numberOfTasks ) )
			using ( TestBufferFiller testTaskBufferFiller = new TestBufferFiller( taskBuffer ) )
			using ( StandardTaskPoller poller = new StandardTaskPoller( processingOpts,
				taskQueueConsumer,
				taskQueueProducer,
				taskBuffer,
				new StandardTaskPollerMetricsProvider() ) )
			{
				testTaskBufferFiller.FillBuffer();

				await poller.StartAsync();
				await GenerateTaskBufferFakeTaskRetrieveEventsAsync( taskBuffer );
				await poller.StopAync();

				Assert.AreEqual( taskBuffer.RefusedElementCount,
					taskQueueConsumer.ActuallyDequeuedElementsCount );

				Assert.AreEqual( taskBuffer.RefusedElementCount,
					taskQueueProducer.ProducedTasksCount );

				Assert.IsTrue( taskBuffer.IsFull );
			}
		}

		private async Task GenerateTaskBufferFakeTaskRetrieveEventsAsync( MockTaskBuffer taskBuffer )
		{
			int fakeTaskRetrieveEventsCount =
				GenerateFakeTaskRetreiveEventsCount();

			for ( int i = 0; i < fakeTaskRetrieveEventsCount; i++ )
			{
				taskBuffer.FakeNotifyQueuedTaskRetrieved();
				await Task.Delay( GenerateMilliseconDelayAmount() );
			}
		}

		private int GenerateFakeTaskRetreiveEventsCount()
		{
			Faker faker = new Faker();
			return faker.Random.Int( 1, 10 );
		}
	}
}
