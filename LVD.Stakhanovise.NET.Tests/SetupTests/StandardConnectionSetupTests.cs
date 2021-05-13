using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Setup;
using Bogus;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class StandardConnectionSetupTests
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
			Faker faker = new Faker();
			StandardConnectionSetup setup = new StandardConnectionSetup();

			setup.WithConnectionKeepAlive( faker.Random.Int( 0, 250 ) );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryCount( faker.Random.Int( 0, 10 ) );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryDelayMilliseconds( faker.Random.Int( 100, 1000 ) );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionString( faker.Random.String( 250 ) );
			Assert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			Assert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			Assert.IsTrue( setup.IsConnectionStringUserConfigured );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_ConfiguredInstance_CorrectlyBuildsConnectionOptions ()
		{
			Faker faker = new Faker();
			StandardConnectionSetup setup = new StandardConnectionSetup();

			string connectionString = faker.Random.String( 250 );
			int connectionKeepAliveSeconds = faker.Random.Int( 0, 250 );
			int connectionRetryCount = faker.Random.Int( 0, 10 );
			int connectionRetryDelayMilliseconds = faker.Random.Int( 100, 1000 );

			setup.WithConnectionKeepAlive( connectionKeepAliveSeconds )
				.WithConnectionRetryCount( connectionRetryCount )
				.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds )
				.WithConnectionString( connectionString );

			ConnectionOptions options = setup.BuildOptions();

			Assert.NotNull( options );
			Assert.AreEqual( connectionString, 
				options.ConnectionString );
			Assert.AreEqual( connectionKeepAliveSeconds, 
				options.ConnectionKeepAliveSeconds );
			Assert.AreEqual( connectionRetryCount, 
				options.ConnectionRetryCount );
			Assert.AreEqual( connectionRetryDelayMilliseconds, 
				options.ConnectionRetryDelayMilliseconds );
		}
	}
}
