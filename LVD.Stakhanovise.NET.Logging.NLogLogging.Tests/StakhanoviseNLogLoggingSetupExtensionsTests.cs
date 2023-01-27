using LVD.Stakhanovise.NET.Setup;
using Moq;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging.Tests
{
	[TestFixture]
	public class StakhanoviseNLogLoggingSetupExtensionsTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanRegisterLoggingProvider_NonNullTargetSetup()
		{
			Mock<IStakhanoviseSetup> setupMock =
				CreateSetupMock();

			IStakhanoviseSetup targetSetup =
				setupMock.Object;

			IStakhanoviseSetup chainedSetup =
				targetSetup.WithNLogLogging();

			Assert.AreSame( targetSetup,
				chainedSetup );

			setupMock.VerifyAll();
			setupMock.VerifyNoOtherCalls();
		}

		[Test]
		public void Test_CanRegisterLoggingProvider_NullTargetSetup()
		{
			IStakhanoviseSetup targetSetup = null;
			Assert.Throws<ArgumentNullException>( () => targetSetup.WithNLogLogging() );
		}

		private Mock<IStakhanoviseSetup> CreateSetupMock()
		{
			Mock<IStakhanoviseSetup> mock = new Mock<IStakhanoviseSetup>( MockBehavior.Strict );

			mock.Setup( m => m.WithLoggingProvider( It.IsAny<StakhanoviseNLogLoggingProvider>() ) )
				.Returns( mock.Object );

			return mock;
		}
	}
}
