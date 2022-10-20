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
using LVD.Stakhanovise.NET.Tests.Helpers;

namespace LVD.Stakhanovise.NET.Tests.WorkerTests
{
	[TestFixture]
	public class StandardTaskExecutionResultProcessorTests
	{
		private const int FaultErrorThresholdCount = 3;

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanProcessResult_Successful()
		{
			QueuedTaskToken queuedTaskToken =
				CreateQueuedTaskToken();

			TaskExecutionResult executionResult =
				GenerateSuccessfulResult();

			Mock<ITaskResultQueue> resultQueueMock =
				CreateResultQueueMock( queuedTaskToken );

			Mock<IExecutionPerformanceMonitor> perfMonMock =
				CreateExecutionPerformanceMonitorMock( queuedTaskToken,
					executionResult );

			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>();

			resultQueueMock.Setup( rq => rq.PostResultAsync( queuedTaskToken ) )
				.Verifiable();

			StandardTaskExecutionResultProcessor processor =
				CreateTestResultProcessorInstance( resultQueueMock,
					producerMock,
					perfMonMock );

			TaskProcessingResult processingResult =
				new TaskProcessingResult( queuedTaskToken,
					executionResult );

			await processor.ProcessResultAsync( processingResult );

			AssertResultQueuePostCalled( resultQueueMock,
				queuedTaskToken );

			AssertPerfMonMockReportCalled( perfMonMock,
				queuedTaskToken,
				executionResult );

			AssertProducerEnqueueNotCalled( producerMock );
		}

		private QueuedTaskToken CreateQueuedTaskToken( int errorCount = 0 )
		{
			return QueuedTaskHelpers.CreateQueuedTaskToken( errorCount );
		}

		private TaskExecutionResult GenerateSuccessfulResult()
		{
			return new TaskExecutionResult(
				TaskExecutionResultInfo.Successful(),
				RandomExecutionTime(),
				DateTimeOffset.UtcNow,
				FaultErrorThresholdCount );
		}

		private Mock<ITaskResultQueue> CreateResultQueueMock( QueuedTaskToken queuedTaskToken )
		{
			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>();

			resultQueueMock.Setup( rq => rq.PostResultAsync( queuedTaskToken ) )
				.Verifiable();

			return resultQueueMock;
		}

		private Mock<IExecutionPerformanceMonitor> CreateExecutionPerformanceMonitorMock( QueuedTaskToken queuedTaskToken,
			TaskExecutionResult executionResult )
		{
			Mock<IExecutionPerformanceMonitor> perfMonMock = new
				Mock<IExecutionPerformanceMonitor>();

			perfMonMock.Setup( pm => pm.ReportExecutionTimeAsync(
					queuedTaskToken.DequeuedTask.Type,
					executionResult.ProcessingTimeMilliseconds,
					0
				) )
				.Verifiable();

			return perfMonMock;
		}

		private StandardTaskExecutionResultProcessor CreateTestResultProcessorInstance( Mock<ITaskResultQueue> resultQueueMock,
			Mock<ITaskQueueProducer> producerMock,
			Mock<IExecutionPerformanceMonitor> perfMonMock )
		{
			return new StandardTaskExecutionResultProcessor( resultQueueMock.Object,
				producerMock.Object,
				perfMonMock.Object,
				CreateLogger() );
		}

		private void AssertResultQueuePostCalled( Mock<ITaskResultQueue> resultQueueMock,
			QueuedTaskToken queuedTaskToken )
		{
			resultQueueMock.Verify( rq => rq.PostResultAsync( queuedTaskToken ), Times.Once() );
			resultQueueMock.VerifyNoOtherCalls();
		}

		private void AssertProducerEnqueueNotCalled( Mock<ITaskQueueProducer> producerMock )
		{
			producerMock.Verify( pm => pm.EnqueueAsync( It.IsAny<QueuedTaskProduceInfo>() ), Times.Never() );
			producerMock.VerifyNoOtherCalls();
		}

		private void AssertPerfMonMockReportCalled( Mock<IExecutionPerformanceMonitor> perfMonMock,
			QueuedTaskToken queuedTaskToken,
			TaskExecutionResult executionResult )
		{
			perfMonMock.Verify( pm => pm.ReportExecutionTimeAsync(
					queuedTaskToken.DequeuedTask.Type,
					executionResult.ProcessingTimeMilliseconds,
					0
				), Times.Once() );

			perfMonMock.VerifyNoOtherCalls();
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanProcessResult_Cancelled()
		{
			QueuedTaskToken queuedTaskToken =
				CreateQueuedTaskToken();

			TaskExecutionResult executionResult =
				GenerateCancelledResult();

			Mock<ITaskResultQueue> resultQueueMock =
				new Mock<ITaskResultQueue>();

			Mock<IExecutionPerformanceMonitor> perfMonMock =
				CreateExecutionPerformanceMonitorMock( queuedTaskToken,
					executionResult );

			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>();

			StandardTaskExecutionResultProcessor processor =
				CreateTestResultProcessorInstance( resultQueueMock,
					producerMock,
					perfMonMock );

			TaskProcessingResult processingResult =
				new TaskProcessingResult( queuedTaskToken,
					executionResult );

			await processor.ProcessResultAsync( processingResult );

			AssertResultQueuePostNotCalled( resultQueueMock );

			AssertPerfMonMockReportCalled( perfMonMock,
				queuedTaskToken,
				executionResult );

			AssertProducerEnqueueNotCalled( producerMock );
		}

		private TaskExecutionResult GenerateCancelledResult()
		{
			return new TaskExecutionResult(
				TaskExecutionResultInfo.Cancelled(),
				RandomExecutionTime(),
				DateTimeOffset.UtcNow,
				FaultErrorThresholdCount );
		}

		private void AssertResultQueuePostNotCalled( Mock<ITaskResultQueue> resultQueueMock )
		{
			resultQueueMock.Verify( rq => rq.PostResultAsync( It.IsAny<IQueuedTaskToken>() ), Times.Never() );
			resultQueueMock.VerifyNoOtherCalls();
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 3 )]
		[Repeat( 10 )]
		public async Task Test_CanProcessResult_ExecutedWithError_NotFatal( int currentErrorCount )
		{
			QueuedTaskToken queuedTaskToken =
				CreateQueuedTaskToken( currentErrorCount );

			TaskExecutionResult executionResult =
				GenerateFailedResult();

			Mock<ITaskResultQueue> resultQueueMock =
				CreateResultQueueMock( queuedTaskToken );

			Mock<IExecutionPerformanceMonitor> perfMonMock =
				CreateExecutionPerformanceMonitorMock( queuedTaskToken,
					executionResult );

			Mock<ITaskQueueProducer> producerMock =
				CreateQueueProducerMock( queuedTaskToken,
					executionResult );

			StandardTaskExecutionResultProcessor processor =
				CreateTestResultProcessorInstance( resultQueueMock,
					producerMock,
					perfMonMock );

			TaskProcessingResult processingResult =
				new TaskProcessingResult( queuedTaskToken,
					executionResult );

			await processor.ProcessResultAsync( processingResult );

			AssertResultQueuePostCalled( resultQueueMock,
				queuedTaskToken );

			AssertPerfMonMockReportCalled( perfMonMock,
				queuedTaskToken,
				executionResult );

			AssertProducerEnqueueCalled( producerMock,
				queuedTaskToken,
				executionResult );
		}

		private TaskExecutionResult GenerateFailedResult()
		{
			return new TaskExecutionResult(
				TaskExecutionResultInfo.ExecutedWithError( new QueuedTaskError( "Some error" ), true ),
				RandomExecutionTime(),
				RandomFutureRetryTime(),
				FaultErrorThresholdCount );
		}

		private Mock<ITaskQueueProducer> CreateQueueProducerMock( QueuedTaskToken queuedTaskToken,
			TaskExecutionResult executionResult )
		{
			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>();

			producerMock.Setup( pm => pm.EnqueueAsync(
					It.Is<QueuedTaskProduceInfo>( pi => ProduceInfoMatchesTokenAndResult( pi,
						queuedTaskToken,
						executionResult ) )
				) );

			return producerMock;
		}

		private void AssertProducerEnqueueCalled( Mock<ITaskQueueProducer> producerMock,
			QueuedTaskToken queuedTaskToken,
			TaskExecutionResult executionResult )
		{
			producerMock.Verify( pm => pm.EnqueueAsync(
					It.Is<QueuedTaskProduceInfo>( pi => ProduceInfoMatchesTokenAndResult( pi,
						queuedTaskToken,
						executionResult ) )
				), Times.Once() );

			producerMock.VerifyNoOtherCalls();
		}

		private bool ProduceInfoMatchesTokenAndResult( QueuedTaskProduceInfo produceInfo,
			QueuedTaskToken queuedTaskToken,
			TaskExecutionResult executionResult )
		{
			return produceInfo.Id == queuedTaskToken.DequeuedTask.Id
				&& produceInfo.Type == queuedTaskToken.DequeuedTask.Type
				&& produceInfo.Payload == queuedTaskToken.DequeuedTask.Payload
				&& produceInfo.Source == queuedTaskToken.DequeuedTask.Source
				&& produceInfo.Status == queuedTaskToken.LastQueuedTaskResult.Status
				&& produceInfo.LockedUntilTs == executionResult.RetryAt;
		}

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanProcessResult_ExecutedWithError_Fatal()
		{
			QueuedTaskToken queuedTaskToken =
				CreateQueuedTaskToken( FaultErrorThresholdCount + 2 );

			TaskExecutionResult executionResult =
				GenerateFailedResult();

			Mock<ITaskResultQueue> resultQueueMock =
				CreateResultQueueMock( queuedTaskToken );

			Mock<IExecutionPerformanceMonitor> perfMonMock =
				CreateExecutionPerformanceMonitorMock( queuedTaskToken,
					executionResult );

			Mock<ITaskQueueProducer> producerMock =
				new Mock<ITaskQueueProducer>();

			StandardTaskExecutionResultProcessor processor =
				CreateTestResultProcessorInstance( resultQueueMock,
					producerMock,
					perfMonMock );

			TaskProcessingResult processingResult =
				new TaskProcessingResult( queuedTaskToken,
					executionResult );

			await processor.ProcessResultAsync( processingResult );

			AssertResultQueuePostCalled( resultQueueMock,
				queuedTaskToken );

			AssertPerfMonMockReportCalled( perfMonMock,
				queuedTaskToken,
				executionResult );

			AssertProducerEnqueueNotCalled( producerMock );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}

		private TimeSpan RandomExecutionTime()
		{
			Faker faker = new Faker();
			long milliseconds = faker.Random
				.Long( 0, 100000 );

			return TimeSpan
				.FromMilliseconds( milliseconds );
		}

		private DateTimeOffset RandomFutureRetryTime()
		{
			Faker faker = new Faker();
			return faker.Date.FutureOffset();
		}
	}
}
