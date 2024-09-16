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
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.BufferTests
{
	[TestFixture]
	public class StandardTaskBufferTests
	{
		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_DefaultTaskBuffer_InitialStateIsCorrect( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				ClassicAssert.IsFalse( buffer.HasTasks );
				ClassicAssert.IsFalse( buffer.IsFull );
				ClassicAssert.AreEqual( capacity, buffer.Capacity );
				ClassicAssert.IsFalse( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_CanAdd_NonFullBuffer( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				for ( int i = 0; i < buffer.Capacity; i++ )
					ClassicAssert.IsTrue( buffer.TryAddNewTask( CreateMockQueuedTaskToken() ) );

				ClassicAssert.AreEqual( buffer.Capacity, buffer.Count );
				ClassicAssert.IsTrue( buffer.IsFull );
				ClassicAssert.IsFalse( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public void Test_CanGet_FromNonEmptyBuffer( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				//Add some items
				int actualItemNumber =
					GenerateItemNumber( capacity );

				List<IQueuedTaskToken> addedTasks =
					GenerateAndAddItemsToBuffer( buffer,
						actualItemNumber );

				for ( int i = 0; i < actualItemNumber; i++ )
				{
					IQueuedTaskToken queuedTaskToken = buffer
						.TryGetNextTask();

					ClassicAssert.NotNull( queuedTaskToken );
					ClassicAssert.IsTrue( addedTasks.Any( t => t.DequeuedTask.Id
						== queuedTaskToken.DequeuedTask.Id ) );
				}

				ClassicAssert.IsFalse( buffer.IsFull );
				ClassicAssert.IsFalse( buffer.IsCompleted );
			}
		}

		private List<IQueuedTaskToken> GenerateAndAddItemsToBuffer( StandardTaskBuffer buffer, int itemCount )
		{
			List<IQueuedTaskToken> addedTasks = new List<IQueuedTaskToken>();

			for ( int i = 0; i < itemCount; i++ )
			{
				addedTasks.Add( CreateMockQueuedTaskToken() );
				buffer.TryAddNewTask( addedTasks [ i ] );
			}

			return addedTasks;
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_TryGet_FromEmptyBuffer( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				ClassicAssert.IsNull( buffer.TryGetNextTask() );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_TryAdd_ToFullBuffer( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				FillBuffer( buffer );
				//Now attempt to add one more
				ClassicAssert.IsFalse( buffer.TryAddNewTask( CreateMockQueuedTaskToken() ) );
			}
		}

		private void FillBuffer( StandardTaskBuffer buffer )
		{
			GenerateAndAddItemsToBuffer( buffer, buffer.Capacity );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_CanCompleteAdding( int capacity )
		{
			using ( StandardTaskBuffer buffer = CreateBuffer( capacity ) )
			{
				int actualItemNumber =
					GenerateItemNumber( capacity );

				GenerateAndAddItemsToBuffer( buffer,
					actualItemNumber );

				//Complete adding
				buffer.CompleteAdding();

				if ( buffer.Count > 0 )
					ClassicAssert.IsFalse( buffer.IsCompleted );
				else
					ClassicAssert.IsTrue( buffer.IsCompleted );

				//We can no longer add items
				ClassicAssert.IsFalse( buffer.TryAddNewTask( CreateMockQueuedTaskToken() ) );

				//We must be able to retrieve the other items
				for ( int i = 0; i < actualItemNumber; i++ )
					ClassicAssert.NotNull( buffer.TryGetNextTask() );

				//Now it must be marked as completed
				ClassicAssert.IsTrue( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1, 1 )]
		[TestCase( 1, 5 )]
		[TestCase( 5, 1 )]
		[TestCase( 5, 10 )]
		[TestCase( 10, 5 )]
		[TestCase( 5, 5 )]
		[Repeat( 5 )]
		public void Test_ConsumerProducerScenario( int nProducers, int nConsumers )
		{
			ConsumerProducerScenarioTestRunner runner =
				new ConsumerProducerScenarioTestRunner( nProducers,
					nConsumers );

			using ( StandardTaskBuffer buffer = CreateBuffer( 10 ) )
				runner.RunTests( buffer );

			AssertTaskTokensListMatchesExpectedCount
				.For( runner.TotalProducedTasks )
				.Check( runner.ProcessedTasks );
		}

		[Test]
		public void Test_CanRaiseItemAdditionEvents()
		{
			bool handlerCalled = false;

			using ( StandardTaskBuffer buffer = CreateBuffer( 10 ) )
			{
				buffer.QueuedTaskAdded += ( s, e ) => handlerCalled = true;
				buffer.TryAddNewTask( CreateMockQueuedTaskToken() );
			}

			ClassicAssert.IsTrue( handlerCalled );
		}

		[Test]
		public void Test_CanRaiseItemRemovalEvents()
		{
			bool handlerCalled = false;

			using ( StandardTaskBuffer buffer = CreateBuffer( 10 ) )
			{
				buffer.QueuedTaskRetrieved += ( s, e ) => handlerCalled = true;

				buffer.TryAddNewTask( CreateMockQueuedTaskToken() );
				buffer.TryGetNextTask();
			}

			ClassicAssert.IsTrue( handlerCalled );
		}

		private StandardTaskBuffer CreateBuffer(int capacity)
		{
			return new StandardTaskBuffer( capacity, new StandardTaskBufferMetricsProvider() );
		}

		private int GenerateItemNumber( int capacity )
		{
			Random random = new Random();
			int subtract = random.Next( 1, Math.Max( 1, capacity / 2 ) );
			int actualItemNumber = capacity - subtract;
			return actualItemNumber;
		}

		private IQueuedTaskToken CreateMockQueuedTaskToken()
		{
			return new MockQueuedTaskToken( Guid.NewGuid() );
		}
	}
}
