using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Setup;
using Bogus;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Tests.SetupTests.Support;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class StandardConnectionSetupTests : BaseConnectionSetupTests
	{
		[Test]
		public void Test_NewInstance_ReportsAllNotConfigured ()
		{
			StandardConnectionSetup setup = new StandardConnectionSetup();
			Assert.IsFalse( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_ConfiguredInstance_CorrectlyReportsConfiguredMembers ()
		{
			StandardConnectionSetup setup = new StandardConnectionSetup();
			ConnectionOptionsSourceData sourceData = GenerateConnectionOptionsData();

			setup.WithConnectionKeepAlive( sourceData.ConnectionKeepAliveSeconds );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryCount( sourceData.ConnectionRetryCount );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryDelayMilliseconds( sourceData.ConnectionRetryDelayMilliseconds );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionString( sourceData.ConnectionString );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionStringUserConfigured );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_ConfiguredInstance_CorrectlyBuildsConnectionOptions ()
		{
			StandardConnectionSetup setup = new StandardConnectionSetup();
			ConnectionOptionsSourceData sourceData = GenerateConnectionOptionsData();

			ConfigureSetupWithSourceData( setup, 
				sourceData );

			ConnectionOptions options = setup
				.BuildOptions();

			AssertConnectionOptionsMatchesSourceData( sourceData, 
				options );
		}
	}
}
