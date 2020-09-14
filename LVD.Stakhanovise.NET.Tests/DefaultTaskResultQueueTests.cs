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
