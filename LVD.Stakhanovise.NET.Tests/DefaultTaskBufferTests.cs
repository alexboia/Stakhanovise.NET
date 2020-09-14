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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class DefaultTaskBufferTests
	{
		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_DefaultTaskBuffer_InitialStateIsCorrect ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				Assert.IsFalse( buffer.HasTasks );
				Assert.IsFalse( buffer.IsFull );
				Assert.AreEqual( capacity, buffer.Capacity );
				Assert.IsFalse( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_CanAdd_NonFullBuffer ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				for ( int i = 0; i < buffer.Capacity; i++ )
					Assert.IsTrue( buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) ) );

				Assert.AreEqual( buffer.Capacity, buffer.Count );
				Assert.IsTrue( buffer.IsFull );
				Assert.IsFalse( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public void Test_CanGet_FromNonEmptyBuffer ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				//Add some items
				int actualItemNumber = ProduceItemNumber( capacity );
				List<QueuedTask> addedTasks = new List<QueuedTask>();

				for ( int i = 0; i < actualItemNumber; i++ )
				{
					addedTasks.Add( new QueuedTask( Guid.NewGuid() ) );
					buffer.TryAddNewTask( addedTasks[ i ] );
				}

				for ( int i = 0; i < actualItemNumber; i++ )
				{
					QueuedTask queuedTask = buffer.TryGetNextTask();

					Assert.NotNull( queuedTask );
					Assert.IsTrue( addedTasks.Any( t => t.Id == queuedTask.Id ) );
				}

				Assert.IsFalse( buffer.IsFull );
				Assert.IsFalse( buffer.IsCompleted );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_TryGet_FromEmptyBuffer ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				Assert.IsNull( buffer.TryGetNextTask() );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_TryAdd_ToFullBuffer ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				//Fill buffer
				for ( int i = 0; i < buffer.Capacity; i++ )
					buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) );

				//Now attempt to add one more
				Assert.IsFalse( buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) ) );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		public void Test_CanCompleteAdding ( int capacity )
		{
			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( capacity ) )
			{
				int actualItemNumber = ProduceItemNumber( capacity );

				for ( int i = 0; i < actualItemNumber; i++ )
					Assert.IsTrue( buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) ) );

				//Complete adding
				buffer.CompleteAdding();

				if ( buffer.Count > 0 )
					Assert.IsFalse( buffer.IsCompleted );
				else
					Assert.IsTrue( buffer.IsCompleted );

				//We can no longer add items
				Assert.IsFalse( buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) ) );

				//We must be able to retrieve the other items
				for ( int i = 0; i < actualItemNumber; i++ )
					Assert.NotNull( buffer.TryGetNextTask() );

				//Now it must be marked as completed
				Assert.IsTrue( buffer.IsCompleted );
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
		public void Test_ConsumerProducerScenario ( int nProducers, int nConsumers )
		{
			Task coordinator;
			Task[] allProducers = new Task[ nProducers ];
			Task[] allConsumers = new Task[ nConsumers ];

			int expectedTotal = 0;
			ConcurrentBag<QueuedTask> processedTasks = new ConcurrentBag<QueuedTask>();

			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( 10 ) )
			{
				for ( int iProducer = 0; iProducer < nProducers; iProducer++ )
				{
					allProducers[ iProducer ] = Task.Run( () =>
					{
						//Generate a number of items to produce 
						// and add that to the expected total
						int nItems = new Random().Next( 1, 100 );
						Interlocked.Add( ref expectedTotal, nItems );

						while ( nItems > 0 )
						{
							bool isAdded = buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) );
							if ( isAdded )
								nItems--;
							else
								Task.Delay( 10 ).Wait();
						}
					} );
				}

				for ( int iConsumer = 0; iConsumer < nConsumers; iConsumer++ )
				{
					allConsumers[ iConsumer ] = Task.Run( () =>
					{
						//Consumers run until the buffer is completed:
						//  - marked as completed with respect to additons
						//      AND
						//  - has no more items
						while ( !buffer.IsCompleted )
						{
							QueuedTask queuedTask = buffer.TryGetNextTask();
							if ( queuedTask != null )
								processedTasks.Add( queuedTask );
							else
								Task.Delay( 10 ).Wait();
						}
					} );
				}

				coordinator = Task.Run( () =>
				{
					//The coordinator waits for all producers 
					//  to finish and then marks buffer 
					//  addition operations as being completed
					Task.WaitAll( allProducers );
					buffer.CompleteAdding();
				} );

				//Wait for all threads to stop
				Task.WaitAll( coordinator );
				Task.WaitAll( allConsumers );

				//Check that:
				//  a) we have the exact number of items we added
				//  b) there are no items that have been processed two times

				Assert.AreEqual( expectedTotal,
					processedTasks.Count );

				foreach ( QueuedTask queuedTask in processedTasks )
					Assert.AreEqual( 1, processedTasks.Count( t => t.Id == queuedTask.Id ) );
			}
		}

		[Test]
		public void Test_CanRaiseItemAdditionEvents ()
		{
			bool handlerCalled = false;

			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( 10 ) )
			{
				buffer.QueuedTaskAdded += ( s, e ) => handlerCalled = true;
				buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) );
			}

			Assert.IsTrue( handlerCalled );
		}

		[Test]
		public void Test_CanRaiseItemRemovalEvents ()
		{
			bool handlerCalled = false;

			using ( DefaultTaskBuffer buffer = new DefaultTaskBuffer( 10 ) )
			{
				buffer.QueuedTaskRetrieved += ( s, e ) => handlerCalled = true;

				buffer.TryAddNewTask( new QueuedTask( Guid.NewGuid() ) );
				buffer.TryGetNextTask();
			}

			Assert.IsTrue( handlerCalled );
		}

		private int ProduceItemNumber ( int capacity )
		{
			Random random = new Random();
			int subtract = random.Next( 1, Math.Max( 1, capacity / 2 ) );
			int actualItemNumber = capacity - subtract;
			return actualItemNumber;
		}
	}
}
