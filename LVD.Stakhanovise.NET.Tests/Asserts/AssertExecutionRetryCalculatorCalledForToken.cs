using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertExecutionRetryCalculatorCalledForToken
	{
		private IQueuedTaskToken mTaskToken;

		private AssertExecutionRetryCalculatorCalledForToken( IQueuedTaskToken taskToken )
		{
			mTaskToken = taskToken
				?? throw new ArgumentNullException( nameof( taskToken ) );
		}

		public static AssertExecutionRetryCalculatorCalledForToken For( IQueuedTaskToken taskToken )
		{
			return new AssertExecutionRetryCalculatorCalledForToken( taskToken );
		}

		public void Check( ITaskExecutionRetryCalculator resolver )
		{
			Mock<ITaskExecutionRetryCalculator> resolverMock = Mock
				.Get<ITaskExecutionRetryCalculator>( resolver );

			Check( resolverMock );
		}

		public void Check( Mock<ITaskExecutionRetryCalculator> calculatorMock )
		{
			calculatorMock.Verify( c => c.ComputeRetryAt( mTaskToken ), Times.Once() );
			calculatorMock.VerifyNoOtherCalls();
		}
	}
}
