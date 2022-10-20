using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertExecutorResolverResolveCalledForToken
	{
		private IQueuedTaskToken mTaskToken;

		private AssertExecutorResolverResolveCalledForToken( IQueuedTaskToken taskToken )
		{
			mTaskToken = taskToken
				?? throw new ArgumentNullException( nameof( taskToken ) );
		}

		public static AssertExecutorResolverResolveCalledForToken For( IQueuedTaskToken taskToken )
		{
			return new AssertExecutorResolverResolveCalledForToken( taskToken );
		}

		public void Check( ITaskExecutorResolver resolver )
		{
			Mock<ITaskExecutorResolver> resolverMock = Mock
				.Get<ITaskExecutorResolver>( resolver );

			Check( resolverMock );
		}

		public void Check( Mock<ITaskExecutorResolver> resolverMock )
		{
			resolverMock.Verify( r => r.ResolveExecutor( mTaskToken.DequeuedTask ), Times.Once() );
			resolverMock.VerifyNoOtherCalls();
		}
	}
}
