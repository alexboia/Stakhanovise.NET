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
			using ( DefaultTaskBuffer taskBuffer = new DefaultTaskBuffer( 100 ) )
			using ( DequeueOnlyMockTaskQueue taskQueue = new DequeueOnlyMockTaskQueue( 0 ) )
			using ( DefaultTaskPoller poller = new DefaultTaskPoller( taskQueue, taskBuffer ) )
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

			using ( DefaultTaskBuffer taskBuffer = new DefaultTaskBuffer( bufferCapacity ) )
			using ( DequeueOnlyMockTaskQueue taskQueue = new DequeueOnlyMockTaskQueue( numberOfTasks ) )
			using ( DefaultTaskPoller poller = new DefaultTaskPoller( taskQueue, taskBuffer ) )
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
