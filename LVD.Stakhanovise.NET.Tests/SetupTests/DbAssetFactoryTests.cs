// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Moq;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Model;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Tests.Support;
using Bogus;
using LVD.Stakhanovise.NET.Setup.Exceptions;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class DbAssetFactoryTests
	{
		[Test]
		[Repeat( 5 )]
		public async Task Test_CanCreateDbAssets_NoErrorThrownBySetup_OneAssetSetup ()
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			ConnectionOptions connectionOptions = GenerateSetupTestDbConnectionOptions();

			await RunDbAssetFactoryTests( connectionOptions,
				mapping,
				count: 1 );
		}

		[Test]
		[Repeat( 5 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		public async Task Test_CanCreateDbAssets_NoErrorThrownBySetup_MultipleAssetSetups ( int assetSetupsCount )
		{
			QueuedTaskMapping mapping = GetDefaultMapping();
			ConnectionOptions connectionOptions = GenerateSetupTestDbConnectionOptions();

			await RunDbAssetFactoryTests( connectionOptions,
				mapping,
				count: assetSetupsCount );
		}

		private async Task RunDbAssetFactoryTests ( ConnectionOptions connectionOptions, QueuedTaskMapping mapping, int count )
		{
			List<Mock<ISetupDbAsset>> dbAssetSetupsMocks = CreateDbAssetSetupMocksWithoutError( connectionOptions,
				mapping,
				count );

			IList<ISetupDbAsset> dbAssetSetups = dbAssetSetupsMocks
				.Select( m => m.Object )
				.ToList();

			DbAssetFactory factory = new DbAssetFactory( dbAssetSetups,
				connectionOptions,
				mapping );

			await factory.CreateDbAssetsAsync();

			foreach ( Mock<ISetupDbAsset> mock in dbAssetSetupsMocks )
			{
				mock.Verify();
				mock.VerifyNoOtherCalls();
			}
		}

		private List<Mock<ISetupDbAsset>> CreateDbAssetSetupMocksWithoutError ( ConnectionOptions connectionOptions, QueuedTaskMapping mapping, int count )
		{
			List<Mock<ISetupDbAsset>> dbAssetMocks = new List<Mock<ISetupDbAsset>>( count );
			for ( int i = 0; i < count; i++ )
				dbAssetMocks.Add( CreateDbAssetSetupMockWithoutError( connectionOptions, mapping ) );
			return dbAssetMocks;
		}

		private Mock<ISetupDbAsset> CreateDbAssetSetupMockWithoutError ( ConnectionOptions connectionOptions, QueuedTaskMapping mapping )
		{
			Mock<ISetupDbAsset> dbAssetMock = new Mock<ISetupDbAsset>();

			dbAssetMock.Setup( m => m.SetupDbAssetAsync( connectionOptions, mapping ) )
				.Returns( Task.CompletedTask )
				.Verifiable();

			return dbAssetMock;
		}

		[Test]
		public async Task Test_CorrectlyWrapsErrorThrownBySetup ()
		{
			Faker faker = new Faker();
			Exception innerExc = faker.System.Exception();
			StakhanoviseSetupException thrownSetupException = null;

			QueuedTaskMapping mapping = GetDefaultMapping();
			ConnectionOptions connectionOptions = GenerateSetupTestDbConnectionOptions();

			Mock<ISetupDbAsset> setupMockWithError =
				CreateDbAssetSetupMockWithError( innerExc );

			IList<ISetupDbAsset> dbAssetSetups = new List<ISetupDbAsset>();
			dbAssetSetups.Add( setupMockWithError.Object );

			DbAssetFactory dbAssetFactory = new DbAssetFactory( dbAssetSetups,
				connectionOptions,
				mapping );

			try
			{
				await dbAssetFactory.CreateDbAssetsAsync();
			}
			catch ( StakhanoviseSetupException exc )
			{
				thrownSetupException = exc;
			}

			Assert.NotNull( thrownSetupException );
			Assert.AreSame( innerExc, thrownSetupException.InnerException );
		}

		private Mock<ISetupDbAsset> CreateDbAssetSetupMockWithError ( Exception exc )
		{
			Mock<ISetupDbAsset> dbAssetMock = new Mock<ISetupDbAsset>();

			dbAssetMock.Setup( m => m.SetupDbAssetAsync( It.IsAny<ConnectionOptions>(), It.IsAny<QueuedTaskMapping>() ) )
				.ThrowsAsync( exc );

			return dbAssetMock;
		}

		private ConnectionOptions GenerateSetupTestDbConnectionOptions ()
		{
			Faker faker = new Faker();
			return new ConnectionOptions( connectionString: faker.System.FilePath(),
				keepAliveSeconds: faker.Random.Int( 0, 10 ),
				retryCount: faker.Random.Int( 0, 10 ),
				retryDelayMilliseconds: faker.Random.Int( 0, 10 ) );
		}

		private QueuedTaskMapping GetDefaultMapping ()
		{
			return new QueuedTaskMapping();
		}
	}
}
