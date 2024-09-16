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
using Bogus;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Tests.SetupTests.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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
			ClassicAssert.NotNull( options );
			ClassicAssert.AreEqual( sourceData.ConnectionString,
				options.ConnectionString );
			ClassicAssert.AreEqual( sourceData.ConnectionKeepAliveSeconds,
				options.ConnectionKeepAliveSeconds );
			ClassicAssert.AreEqual( sourceData.ConnectionRetryCount,
				options.ConnectionRetryCount );
			ClassicAssert.AreEqual( sourceData.ConnectionRetryDelayMilliseconds,
				options.ConnectionRetryDelayMilliseconds );
		}
	}
}
