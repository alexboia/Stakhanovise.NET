using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using Moq;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertExecutionRetryCalculatorNotCalled
	{
		private AssertExecutionRetryCalculatorNotCalled()
		{
			return;
		}

		public static AssertExecutionRetryCalculatorNotCalled EveryTime()
		{
			return new AssertExecutionRetryCalculatorNotCalled();
		}

		public void Check( ITaskExecutionRetryCalculator calculator )
		{
			Mock<ITaskExecutionRetryCalculator> mock = Mock
				.Get<ITaskExecutionRetryCalculator>( calculator );

			Check( mock );
		}

		public void Check( Mock<ITaskExecutionRetryCalculator> calculatorMock )
		{
			calculatorMock.Verify( c => c.ComputeRetryAt( It.IsAny<IQueuedTaskToken>() ), Times.Never() );
			calculatorMock.VerifyNoOtherCalls();
		}
	}
}
