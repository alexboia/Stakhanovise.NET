using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.BufferTests
{
	public class ConsumerProducerScenarioTestRunner
	{
		private Task mCoordinator;

		private Task [] mAllProducers;

		private Task [] mAllConsumers;

		private ConcurrentBag<IQueuedTaskToken> mProcessedTasks =
			new ConcurrentBag<IQueuedTaskToken>();

		private int mTotalProducedTasks = 0;

		public ConsumerProducerScenarioTestRunner( int nProducers, int nConsumers )
		{
			mAllConsumers = new Task [ nConsumers ];
			mAllProducers = new Task [ nProducers ];
		}

		public void RunTests( StandardTaskBuffer buffer )
		{
			Reset();
			StartProducers( buffer );
			StartConsumers( buffer );
			StartCoordinator( buffer );
			WaitAllStopped();
		}

		private void StartProducers( StandardTaskBuffer buffer )
		{
			for ( int iProducer = 0; iProducer < mAllProducers.Length; iProducer++ )
			{
				mAllProducers [ iProducer ] = Task.Run( () =>
				{
					//Generate a number of items to produce 
					// and add that to the expected total
					int nItems = new Random().Next( 1, 100 );
					Interlocked.Add( ref mTotalProducedTasks, nItems );

					while ( nItems > 0 )
					{
						bool isAdded = buffer.TryAddNewTask( CreateMockQueuedTaskToken() );
						if ( isAdded )
							nItems--;
						else
							Task.Delay( 10 ).Wait();
					}
				} );
			}
		}

		private void StartConsumers( StandardTaskBuffer buffer )
		{
			for ( int iConsumer = 0; iConsumer < mAllConsumers.Length; iConsumer++ )
			{
				mAllConsumers [ iConsumer ] = Task.Run( () =>
				{
					//Consumers run until the buffer is completed:
					//  - marked as completed with respect to additons
					//      AND
					//  - has no more items
					while ( !buffer.IsCompleted )
					{
						IQueuedTaskToken queuedTaskToken = buffer.TryGetNextTask();
						if ( queuedTaskToken != null )
							mProcessedTasks.Add( queuedTaskToken );
						else
							Task.Delay( 10 ).Wait();
					}
				} );
			}
		}

		private void StartCoordinator( StandardTaskBuffer buffer )
		{
			mCoordinator = Task.Run( () =>
			{
				//The coordinator waits for all producers 
				//  to finish and then marks buffer 
				//  addition operations as being completed
				Task.WaitAll( mAllProducers );
				buffer.CompleteAdding();
			} );
		}

		private void WaitAllStopped()
		{
			Task.WaitAll( mCoordinator );
			Task.WaitAll( mAllConsumers );
		}

		private void Reset()
		{
			mAllProducers = new Task [ mAllProducers.Length ];
			mAllConsumers = new Task [ mAllConsumers.Length ];
			mProcessedTasks.Clear();
		}

		private IQueuedTaskToken CreateMockQueuedTaskToken()
		{
			return new MockQueuedTaskToken( Guid.NewGuid() );
		}

		public int TotalProducedTasks
			=> mTotalProducedTasks;

		public IEnumerable<IQueuedTaskToken> ProcessedTasks
			=> mProcessedTasks;
	}
}
