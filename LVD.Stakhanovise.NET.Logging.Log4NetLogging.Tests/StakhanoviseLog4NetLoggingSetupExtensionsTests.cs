using LVD.Stakhanovise.NET.Setup;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging.Tests
{
	[TestFixture]
	public class StakhanoviseLog4NetLoggingSetupExtensionsTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanRegisterLoggingProvider_NonNullTargetSetup ()
		{
			Mock<IStakhanoviseSetup> setupMock =
				CreateSetupMock();

			IStakhanoviseSetup targetSetup =
				setupMock.Object;

			IStakhanoviseSetup chainedSetup =
				targetSetup.WithLog4NetLogging();

			ClassicAssert.AreSame( targetSetup,
				chainedSetup );

			setupMock.VerifyAll();
			setupMock.VerifyNoOtherCalls();
		}

		[Test]
		public void Test_CanRegisterLoggingProvider_NullTargetSetup ()
		{
			IStakhanoviseSetup targetSetup = null;
			Assert.Throws<ArgumentNullException>( () => targetSetup.WithLog4NetLogging() );
		}

		private Mock<IStakhanoviseSetup> CreateSetupMock ()
		{
			Mock<IStakhanoviseSetup> mock = new Mock<IStakhanoviseSetup>( MockBehavior.Strict );

			mock.Setup( m => m.WithLoggingProvider( It.IsAny<Log4NetLoggingProvider>() ) )
				.Returns( mock.Object );

			return mock;
		}
	}
}
