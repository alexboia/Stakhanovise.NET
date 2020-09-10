using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class DefaultTaskResultQueueTests
	{
		[Test]
		public async Task Test_CanEnqueueResult_NoResult ()
		{
			QueuedTask queuedTask;
			Mock<ITaskQueueConsumer> taskQueueMock;

			using ( ITaskResultQueue taskResultQueue = CreateResultQueue( out taskQueueMock ) )
			{
				queuedTask = new QueuedTask();
				queuedTask.Id = Guid.NewGuid();

				await taskResultQueue.EnqueueResultAsync( queuedTask, null );

				taskQueueMock.Verify( t => t.ReleaseLockAsync( queuedTask.Id ) );
				taskQueueMock.VerifyNoOtherCalls();
			}
		}

		[Test]
		public async Task Test_CanEnqueueResult_SuccessResult ()
		{
			QueuedTask queuedTask;
			TaskExecutionResult taskExecutionResult;
			Mock<ITaskQueueConsumer> taskQueueMock;

			using ( ITaskResultQueue taskResultQueue = CreateResultQueue( out taskQueueMock ) )
			{
				queuedTask = new QueuedTask( Guid.NewGuid() );
				taskExecutionResult = new TaskExecutionResult( queuedTask );

				await taskResultQueue.EnqueueResultAsync( queuedTask, taskExecutionResult );

				taskQueueMock.Verify( t => t.NotifyTaskCompletedAsync( queuedTask.Id, new TaskExecutionResult( queuedTask ) ) );
				taskQueueMock.VerifyNoOtherCalls();
			}
		}

		[Test]
		public async Task Test_CanEnqueueResult_ErrorResult ()
		{
			QueuedTask queuedTask;
			TaskExecutionResult taskExecutionResult;
			Mock<ITaskQueueConsumer> taskQueueMock;

			using ( ITaskResultQueue taskResultQueue = CreateResultQueue( out taskQueueMock ) )
			{
				queuedTask = new QueuedTask( Guid.NewGuid() );
				taskExecutionResult = new TaskExecutionResult( queuedTask,
					error: new QueuedTaskError( new InvalidOperationException() ),
					isRecoverable: true );

				await taskResultQueue.EnqueueResultAsync( queuedTask, taskExecutionResult );

				taskQueueMock.Verify( t => t.NotifyTaskErroredAsync( queuedTask.Id, taskExecutionResult ) );
				taskQueueMock.VerifyNoOtherCalls();
			}
		}

		private ITaskResultQueue CreateResultQueue ( out Mock<ITaskQueueConsumer> taskQueueMock )
		{
			Mock<ITaskQueueConsumer> mock = new Mock<ITaskQueueConsumer>( MockBehavior.Strict );

			mock.Setup( t => t.ReleaseLockAsync( It.IsAny<Guid>() ) );
			mock.Setup( t => t.NotifyTaskCompletedAsync( It.IsAny<Guid>(), It.IsAny<TaskExecutionResult>() ) );

			mock.Setup( t => t.NotifyTaskErroredAsync( It.IsAny<Guid>(),
				 It.IsNotNull<TaskExecutionResult>() ) );

			taskQueueMock = mock;
			return new DefaultTaskResultQueue( mock.Object );
		}
	}
}
