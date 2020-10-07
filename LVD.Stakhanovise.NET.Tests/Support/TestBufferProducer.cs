using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestBufferProducer
	{
		private ITaskBuffer mTaskBuffer;

		List<IQueuedTaskToken> mProducedTasks = new List<IQueuedTaskToken>();

		private Type[] mPayloadTypes;

		public TestBufferProducer ( ITaskBuffer buffer, Type[] payloadTypes )
		{
			mTaskBuffer = buffer;
			mPayloadTypes = payloadTypes;
		}

		public Task ProduceTasksAsync ( int numberOfTasks )
		{
			ManualResetEvent bufferSpaceAvailableWaitHandle =
				new ManualResetEvent( false );

			Queue<Type> taskPayloadTypes =
				new Queue<Type>( mPayloadTypes );

			return Task.Run( () =>
			{
				Type currentPayloadType;
				IQueuedTaskToken newTaskToken;
				QueuedTask newTask;
				QueuedTaskResult newLastTaskResult;

				EventHandler handleBufferElementRemoved
					= ( s, e ) => bufferSpaceAvailableWaitHandle.Set();

				mTaskBuffer.QueuedTaskRetrieved
					+= handleBufferElementRemoved;

				while ( taskPayloadTypes.TryDequeue( out currentPayloadType ) )
				{
					for ( int i = 0; i < numberOfTasks; i++ )
					{
						newTask = new QueuedTask( Guid.NewGuid() )
						{
							Payload = Activator.CreateInstance( currentPayloadType ),
							Type = currentPayloadType.FullName
						};

						newLastTaskResult = new QueuedTaskResult( newTask )
						{
							Status = QueuedTaskStatus.Unprocessed
						};

						newTaskToken = new MockQueuedTaskToken( newTask, newLastTaskResult );

						mProducedTasks.Add( newTaskToken );

						while ( !mTaskBuffer.TryAddNewTask( newTaskToken ) )
						{
							bufferSpaceAvailableWaitHandle.WaitOne();
							bufferSpaceAvailableWaitHandle.Reset();
						}
					}
				}

				mTaskBuffer.CompleteAdding();
				mTaskBuffer.QueuedTaskRetrieved
					-= handleBufferElementRemoved;
			} );
		}

		public void AssertMatchesProcessedTasks ( IEnumerable<IQueuedTaskToken> processedTaskTokens )
		{
			Assert.AreEqual( mProducedTasks.Count,
				processedTaskTokens.Count() );

			foreach ( IQueuedTaskToken produced in mProducedTasks )
				Assert.NotNull( processedTaskTokens.FirstOrDefault(
					t => t.DequeuedTask.Id == produced.DequeuedTask.Id ) );
		}
	}
}
