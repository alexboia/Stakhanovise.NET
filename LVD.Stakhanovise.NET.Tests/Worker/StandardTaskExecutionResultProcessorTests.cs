using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Tests.Payloads;
using Bogus;
using LVD.Stakhanovise.NET.Processor;

namespace LVD.Stakhanovise.NET.Tests.Worker
{
	[TestFixture]
	public class StandardTaskExecutionResultProcessorTests
	{
		[Test]
		public async Task Test_CanProcessResult_WhenHasResult_AndNotCancelled()
		{
			QueuedTaskToken queuedTaskToken =
				CreateQueuedTaskToken();

			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>();

			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>();

			Mock<IExecutionPerformanceMonitor> perfMonMock = new
				Mock<IExecutionPerformanceMonitor>();

			TaskExecutionResult result =
				new TaskExecutionResult(
					TaskExecutionResultInfo.Successful(),
					TimeSpan.FromSeconds( 10 ),
					DateTimeOffset.UtcNow,
					3 );

			resultQueueMock.Setup( rq => rq.PostResultAsync( queuedTaskToken ) )
				.Verifiable();

			perfMonMock.Setup( pm => pm.ReportExecutionTimeAsync( queuedTaskToken.DequeuedTask.Type,
					result.ProcessingTimeMilliseconds,
					0 ) )
				.Verifiable();

			StandardTaskExecutionResultProcessor processor =
				new StandardTaskExecutionResultProcessor( resultQueueMock.Object,
					producerMock.Object,
					perfMonMock.Object,
					CreateLogger() );

			await processor.ProcessResultAsync( queuedTaskToken,
				result );

			resultQueueMock.Verify();
			resultQueueMock.VerifyNoOtherCalls();

			perfMonMock.Verify();
			perfMonMock.VerifyNoOtherCalls();
		}

		private QueuedTaskToken CreateQueuedTaskToken()
		{
			QueuedTask task = CreateQueuedTask();
			return new QueuedTaskToken( task,
				new QueuedTaskResult( task ),
				DateTimeOffset.UtcNow );
		}

		private QueuedTask CreateQueuedTask()
		{
			SampleTaskPayload payload =
				new SampleTaskPayload();

			return new QueuedTask()
			{
				Id = Guid.NewGuid(),
				Payload = payload,
				Source = Guid.NewGuid()
					.ToString(),
				Type = payload.GetType()
					.FullName,
			};
		}

		[Test]
		public async Task Test_CanProcessResult_WhenHasResult_Cancelled()
		{

		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}
	}
}
