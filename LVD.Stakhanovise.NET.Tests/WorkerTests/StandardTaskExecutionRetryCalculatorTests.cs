using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.WorkerTests.Mocks;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.WorkerTests
{
	[TestFixture]
	public class StandardTaskExecutionRetryCalculatorTests
	{
		private const long AcceptedDeltaMilliseconds = 1;

		[Test]
		[TestCase( 0 )]
		[TestCase( 50 )]
		[TestCase( 100 )]
		[NonParallelizable]
		public void Test_CanComputeRetryAt( int millisecondsDelay )
		{
			RetryCalculationProcessingOptionsMock optionsMock =
				new RetryCalculationProcessingOptionsMock( millisecondsDelay );
			StandardTaskExecutionRetryCalculator calculator =
				new StandardTaskExecutionRetryCalculator( optionsMock.GetOptions(),
					CreateLogger() );

			IQueuedTaskToken queuedTasToken =
				CreateQueuedTaskTokenMock();

			DateTimeOffset expectedRetryAt = DateTimeOffset.UtcNow
				.AddMilliseconds( millisecondsDelay );
			DateTimeOffset retryAt = calculator
				.ComputeRetryAt( queuedTasToken );

			Assert.AreEqual( 1, optionsMock.RetryCalculationCallCount );
			Assert.IsTrue( optionsMock.WasRetryCalculationCalledFor( queuedTasToken ) );

			Assert.GreaterOrEqual( retryAt, expectedRetryAt );
			double deltaMilliseconds = ( retryAt - expectedRetryAt ).TotalMilliseconds;
			Assert.LessOrEqual( deltaMilliseconds, AcceptedDeltaMilliseconds );
		}

		private IQueuedTaskToken CreateQueuedTaskTokenMock()
		{
			return new Mock<IQueuedTaskToken>( MockBehavior.Loose )
				.Object;
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}
	}
}
