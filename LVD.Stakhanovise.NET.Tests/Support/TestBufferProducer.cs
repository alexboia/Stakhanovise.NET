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

		public Task ProduceTasksAsync ( int numberOfTasks, Action<object, TokenReleasedEventArgs> onTokenReleased )
		{
			ManualResetEvent bufferSpaceAvailableWaitHandle =
				new ManualResetEvent( false );

			Queue<Type> taskPayloadTypes =
				new Queue<Type>( mPayloadTypes );

			return Task.Run( () =>
			{
				Type currentPayloadType;
				IQueuedTaskToken newTaskToken;

				EventHandler handleBufferElementRemoved
					= ( s, e ) => bufferSpaceAvailableWaitHandle.Set();
				EventHandler<TokenReleasedEventArgs> handleTokenReleased
					= ( s, e ) => onTokenReleased( s, e );

				mTaskBuffer.QueuedTaskRetrieved
					+= handleBufferElementRemoved;

				while ( taskPayloadTypes.TryDequeue( out currentPayloadType ) )
				{
					for ( int i = 0; i < numberOfTasks; i++ )
					{
						newTaskToken = new MockQueuedTaskToken( new QueuedTask( Guid.NewGuid() )
						{
							Payload = Activator.CreateInstance( currentPayloadType ),
							Status = QueuedTaskStatus.Unprocessed,
							Type = currentPayloadType.FullName
						} );

						newTaskToken.TokenReleased += handleTokenReleased;
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
					t => t.QueuedTask.Id == produced.QueuedTask.Id
					&& ( t.QueuedTask.Status == QueuedTaskStatus.Processed
						|| t.QueuedTask.Status == QueuedTaskStatus.Error
						|| t.QueuedTask.Status == QueuedTaskStatus.Faulted
						|| t.QueuedTask.Status == QueuedTaskStatus.Fatal ) ) );
		}
	}
}
