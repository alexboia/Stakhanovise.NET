using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Setup;
using Bogus;
using LVD.Stakhanovise.NET.Tests.SetupTests.Support;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public abstract class BaseConnectionSetupTests
	{
		protected IConnectionSetup ConfigureSetupWithSourceData ( IConnectionSetup connectionSetup, ConnectionOptionsSourceData sourceData )
		{
			connectionSetup.WithConnectionKeepAlive( sourceData.ConnectionKeepAliveSeconds )
				.WithConnectionRetryCount( sourceData.ConnectionRetryCount )
				.WithConnectionRetryDelayMilliseconds( sourceData.ConnectionRetryDelayMilliseconds )
				.WithConnectionString( sourceData.ConnectionString );

			return connectionSetup;
		}

		protected ConnectionOptionsSourceData GenerateConnectionOptionsData ()
		{
			Faker faker = new Faker();
			return new ConnectionOptionsSourceData()
			{
				ConnectionString = faker.Random.String( 250 ),
				ConnectionKeepAliveSeconds = faker.Random.Int( 0, 250 ),
				ConnectionRetryCount = faker.Random.Int( 0, 10 ),
				ConnectionRetryDelayMilliseconds = faker.Random.Int( 100, 1000 )
			};
		}

		protected void AssertConnectionOptionsMatchesSourceData ( ConnectionOptionsSourceData sourceData,
			ConnectionOptions options )
		{
			Assert.NotNull( options );
			Assert.AreEqual( sourceData.ConnectionString,
				options.ConnectionString );
			Assert.AreEqual( sourceData.ConnectionKeepAliveSeconds,
				options.ConnectionKeepAliveSeconds );
			Assert.AreEqual( sourceData.ConnectionRetryCount,
				options.ConnectionRetryCount );
			Assert.AreEqual( sourceData.ConnectionRetryDelayMilliseconds,
				options.ConnectionRetryDelayMilliseconds );
		}
	}
}
