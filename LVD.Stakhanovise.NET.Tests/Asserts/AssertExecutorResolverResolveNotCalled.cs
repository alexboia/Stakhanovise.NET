using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using Moq;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertExecutorResolverResolveNotCalled
	{
		private AssertExecutorResolverResolveNotCalled()
		{
			return;
		}

		public static AssertExecutorResolverResolveNotCalled EveryTime()
		{
			return new AssertExecutorResolverResolveNotCalled();
		}

		public void Check( ITaskExecutorResolver resolver )
		{
			Mock<ITaskExecutorResolver> resolverMock = Mock
				.Get<ITaskExecutorResolver>( resolver );

			Check( resolverMock );
		}

		public void Check( Mock<ITaskExecutorResolver> resolverMock )
		{
			resolverMock.Verify( r => r.ResolveExecutor( It.IsAny<IQueuedTask>() ),
				Times.Never() );

			resolverMock
				.VerifyNoOtherCalls();
		}
	}
}
