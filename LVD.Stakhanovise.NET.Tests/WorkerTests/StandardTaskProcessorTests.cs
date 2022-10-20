using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using LVD.Stakhanovise.NET.Tests.Executors;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.WorkerTests
{
	[TestFixture]
	public class StandardTaskProcessorTests
	{
		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_NoErrorFromExecutor_WithoutResultFromExecutor()
		{
			await RunSuccessfulProcessingTestsAsync( new SampleTaskPayload() );
		}

		private async Task RunSuccessfulProcessingTestsAsync( object payload )
		{
			CancellationTokenSource stopSource =
				new CancellationTokenSource();

			TaskProcessingOptions options = TestOptions
				.GetDefaultTaskProcessingOptions();

			Mock<ITaskExecutorResolver> resolverMock =
				CreateTaskExecutorResolverMock();

			Mock<ITaskExecutionRetryCalculator> calculatorMock =
				CreateRetryCalculatorMock( options );

			QueuedTaskToken taskToken =
				CreateQueuedTaskToken( payload );

			TaskExecutionContext executionContext =
				new TaskExecutionContext( taskToken,
					stopSource.Token );

			StandardTaskProcessor processor =
				CreateTestTaskProcessor( options,
					resolverMock,
					calculatorMock );

			TaskProcessingResult processingResult = await processor
				.ProcessTaskAsync( executionContext );

			AssertTaskProcessedSuccessfully
				.For( taskToken )
				.Check( processingResult );

			AssertExecutorResolverResolveCalledForToken
				.For( taskToken )
				.Check( resolverMock );

			AssertExecutionRetryCalculatorNotCalled
				.EveryTime()
				.Check( calculatorMock );
		}

		private QueuedTaskToken CreateQueuedTaskToken( object payload, int errorCount = 0 )
		{
			return QueuedTaskHelpers.CreateQueuedTaskToken( payload, errorCount );
		}

		private Mock<ITaskExecutorResolver> CreateTaskExecutorResolverMock()
		{
			Mock<ITaskExecutorResolver> taskExecutorResolverMock =
				new Mock<ITaskExecutorResolver>();

			taskExecutorResolverMock
				.Setup( e => e.ResolveExecutor( It.Is<IQueuedTask>( q => q.IsOfType<SampleTaskPayload>() ) ) )
				.Returns( new SampleTaskPayloadExecutor() );

			taskExecutorResolverMock
				.Setup( e => e.ResolveExecutor( It.Is<IQueuedTask>( q => q.IsOfType<SampleTaskPayloadWithResult>() ) ) )
				.Returns( new SampleTaskPayloadWithResultExecutor() );

			taskExecutorResolverMock
				.Setup( e => e.ResolveExecutor( It.Is<IQueuedTask>( q => q.IsOfType<ErroredTaskPayload>() ) ) )
				.Returns( new ErroredTaskPayloadExecutor() );

			taskExecutorResolverMock
				.Setup( e => e.ResolveExecutor( It.Is<IQueuedTask>( q => q.IsOfType<ThrowsExceptionTaskPayload>() ) ) )
				.Returns( new ThrowsExceptionTaskPayloadExecutor() );

			taskExecutorResolverMock
				.Setup( e => e.ResolveExecutor( It.Is<IQueuedTask>( q => q.IsOfType<CancellationObservedPayload>() ) ) )
				.Returns( new CancellationObservedPayloadExecutor() );

			return taskExecutorResolverMock;
		}

		private Mock<ITaskExecutionRetryCalculator> CreateRetryCalculatorMock( TaskProcessingOptions options )
		{
			DateTimeOffset now = DateTimeOffset
				.UtcNow;

			Mock<ITaskExecutionRetryCalculator> mock =
				new Mock<ITaskExecutionRetryCalculator>();

			mock.Setup( c => c.ComputeRetryAt( It.IsAny<IQueuedTaskToken>() ) )
				.Returns<IQueuedTaskToken>( qt => now.AddMilliseconds( options
					.CalculateRetryMillisecondsDelay
					.Invoke( qt ) ) );

			return mock;
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_NoErrorFromExecutor_WithResultFromExecutor()
		{
			await RunSuccessfulProcessingTestsAsync( new SampleTaskPayloadWithResult() );
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_WithErrorFromExecutor()
		{
			await RunFailedProcessingTestsAsync( new ErroredTaskPayload() );
		}

		private async Task RunFailedProcessingTestsAsync( object payload )
		{
			CancellationTokenSource stopSource =
				new CancellationTokenSource();

			TaskProcessingOptions options = TestOptions
				.GetDefaultTaskProcessingOptions();

			Mock<ITaskExecutorResolver> resolverMock =
				CreateTaskExecutorResolverMock();

			Mock<ITaskExecutionRetryCalculator> calculatorMock =
				CreateRetryCalculatorMock( options );

			QueuedTaskToken taskToken =
				CreateQueuedTaskToken( payload );

			TaskExecutionContext executionContext =
				new TaskExecutionContext( taskToken,
					stopSource.Token );

			StandardTaskProcessor processor =
				CreateTestTaskProcessor( options,
					resolverMock,
					calculatorMock );

			TaskProcessingResult processingResult = await processor
				.ProcessTaskAsync( executionContext );

			AssertTaskProcessedWithError
				.For( taskToken )
				.Check( processingResult );

			AssertExecutorResolverResolveCalledForToken
				.For( taskToken )
				.Check( resolverMock );

			AssertExecutionRetryCalculatorCalledForToken
				.For( taskToken )
				.Check( calculatorMock );
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_WithExceptionFromExecutor()
		{
			await RunFailedProcessingTestsAsync( new ThrowsExceptionTaskPayload() );
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_CancellationRequested_ObservedBeforeExecutorRun()
		{
			CancellationTokenSource stopSource =
				new CancellationTokenSource();

			TaskProcessingOptions options = TestOptions
				.GetDefaultTaskProcessingOptions();

			Mock<ITaskExecutorResolver> resolverMock =
				CreateTaskExecutorResolverMock();

			Mock<ITaskExecutionRetryCalculator> calculatorMock =
				CreateRetryCalculatorMock( options );

			QueuedTaskToken taskToken =
				CreateQueuedTaskToken( new SampleTaskPayload() );

			TaskExecutionContext executionContext =
				new TaskExecutionContext( taskToken,
					stopSource.Token );

			StandardTaskProcessor processor =
				CreateTestTaskProcessor( options,
					resolverMock,
					calculatorMock );

			stopSource.Cancel();

			TaskProcessingResult processingResult = await processor
				.ProcessTaskAsync( executionContext );

			AsserTaskProcessingCancelled
				.For( taskToken )
				.Check( processingResult );

			AssertExecutorResolverResolveNotCalled
				.EveryTime()
				.Check( resolverMock );

			AssertExecutionRetryCalculatorNotCalled
				.EveryTime()
				.Check( calculatorMock );
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorFound_CancellationRequested_ObservedDuringExecutorRun()
		{
			CancellationTokenSource stopSource =
				new CancellationTokenSource();

			TaskProcessingOptions options = TestOptions
				.GetDefaultTaskProcessingOptions();

			Mock<ITaskExecutorResolver> resolverMock =
				CreateTaskExecutorResolverMock();

			Mock<ITaskExecutionRetryCalculator> calculatorMock =
				CreateRetryCalculatorMock( options );

			CancellationObservedPayload payload =
				new CancellationObservedPayload();

			QueuedTaskToken taskToken =
				CreateQueuedTaskToken( payload );

			TaskExecutionContext executionContext =
				new TaskExecutionContext( taskToken,
					stopSource.Token );

			StandardTaskProcessor processor =
				CreateTestTaskProcessor( options,
					resolverMock,
					calculatorMock );

			Task.Delay( 1000 )
				.ContinueWith( a => payload.SendCancellation( stopSource ) )
				.WithoutAwait();

			TaskProcessingResult processingResult = await processor
				.ProcessTaskAsync( executionContext );

			AsserTaskProcessingCancelled
				.For( taskToken )
				.Check( processingResult );

			AssertExecutorResolverResolveCalledForToken
				.For( taskToken )
				.Check( resolverMock );

			AssertExecutionRetryCalculatorNotCalled
				.EveryTime()
				.Check( calculatorMock );
		}

		[Test]
		public async Task Test_CanProcessTask_WhenExecutorNotFound()
		{
			await RunFailedProcessingTestsAsync( new SampleNoExecutorPayload() );
		}

		private StandardTaskProcessor CreateTestTaskProcessor( TaskProcessingOptions options,
			Mock<ITaskExecutorResolver> resolverMock,
			Mock<ITaskExecutionRetryCalculator> calculatorMock )
		{
			return new StandardTaskProcessor( options,
					resolverMock.Object,
					calculatorMock.Object,
					CreateLogger() );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}
	}
}
