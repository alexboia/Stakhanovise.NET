using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Tests.SetupTests.Support;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class CollectiveConnectionSetupTests : BaseConnectionSetupTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanConfigure_OnlyOneManagedInstance_NotAlreadyConfigured ()
		{
			StandardConnectionSetup managedSetup = new StandardConnectionSetup();
			CollectiveConnectionSetup collectiveSetup = new CollectiveConnectionSetup( managedSetup );
			ConnectionOptionsSourceData sourceData = GenerateConnectionOptionsData();

			ConfigureSetupWithSourceData( collectiveSetup,
				sourceData );

			ConnectionOptions options = managedSetup
				.BuildOptions();

			AssertConnectionOptionsMatchesSourceData( sourceData,
				options );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanConfigure_OnlyOneManagedInstance_AlreadyConfigured ()
		{
			StandardConnectionSetup managedSetup = new StandardConnectionSetup();

			CollectiveConnectionSetup collectiveSetup = new CollectiveConnectionSetup( managedSetup );

			ConnectionOptionsSourceData initialSourceData = GenerateConnectionOptionsData();
			ConnectionOptionsSourceData collectiveSourceData = GenerateConnectionOptionsData();

			ConfigureSetupWithSourceData( managedSetup,
				initialSourceData );

			ConfigureSetupWithSourceData( collectiveSetup,
				collectiveSourceData );

			ConnectionOptions options = managedSetup
				.BuildOptions();

			AssertConnectionOptionsMatchesSourceData( initialSourceData,
				options );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_CanConfigure_MultipleManagedInstances_NotAlreadyConfigured ( int instanceCount )
		{
			StandardConnectionSetup[] managedSetups = new StandardConnectionSetup[ instanceCount ];

			for ( int i = 0; i < instanceCount; i++ )
				managedSetups[ i ] = new StandardConnectionSetup();

			CollectiveConnectionSetup collectiveSetup = new CollectiveConnectionSetup( managedSetups );
			ConnectionOptionsSourceData sourceData = GenerateConnectionOptionsData();

			ConfigureSetupWithSourceData( collectiveSetup,
				sourceData );

			for ( int i = 0; i < instanceCount; i++ )
			{
				StandardConnectionSetup managedSetup = managedSetups[ i ];
				ConnectionOptions options = managedSetup
					.BuildOptions();

				AssertConnectionOptionsMatchesSourceData( sourceData,
					options );
			}
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_CanConfigure_MultipleManagedInstances_AlreadyConfigured ( int instanceCount )
		{
			StandardConnectionSetup[] managedSetups = new StandardConnectionSetup[ instanceCount ];
			ConnectionOptionsSourceData[] initialSourceDatas = new ConnectionOptionsSourceData[ instanceCount ];

			for ( int i = 0; i < instanceCount; i++ )
			{
				managedSetups[ i ] = new StandardConnectionSetup();
				initialSourceDatas[ i ] = GenerateConnectionOptionsData();
				ConfigureSetupWithSourceData( managedSetups[ i ], 
					initialSourceDatas[ i ] );
			}

			CollectiveConnectionSetup collectiveSetup = new CollectiveConnectionSetup( managedSetups );
			ConnectionOptionsSourceData collectiveSourceData = GenerateConnectionOptionsData();

			ConfigureSetupWithSourceData( collectiveSetup,
				collectiveSourceData );

			for ( int i = 0; i < instanceCount; i++ )
			{
				StandardConnectionSetup managedSetup = managedSetups[ i ];
				ConnectionOptionsSourceData initialSourceData = initialSourceDatas[ i ];

				ConnectionOptions options = managedSetup
					.BuildOptions();

				AssertConnectionOptionsMatchesSourceData( initialSourceData,
					options );
			}
		}
	}
}
