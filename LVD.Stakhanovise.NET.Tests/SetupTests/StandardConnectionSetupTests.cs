﻿// 
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
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Tests.SetupTests.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class StandardConnectionSetupTests : BaseConnectionSetupTests
	{
		[Test]
		public void Test_NewInstance_ReportsAllNotConfigured ()
		{
			StandardConnectionSetup setup = new StandardConnectionSetup();
			ClassicAssert.IsFalse( setup.IsConnectionKeepAliveSecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionRetryCountUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionStringUserConfigured );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_ConfiguredInstance_CorrectlyReportsConfiguredMembers ()
		{
			StandardConnectionSetup setup = new StandardConnectionSetup();
			ConnectionOptionsSourceData sourceData = GenerateConnectionOptionsData();

			setup.WithConnectionKeepAlive( sourceData.ConnectionKeepAliveSeconds );
			ClassicAssert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionRetryCountUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryCount( sourceData.ConnectionRetryCount );
			ClassicAssert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionRetryDelayMilliseconds( sourceData.ConnectionRetryDelayMilliseconds );
			ClassicAssert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			ClassicAssert.IsFalse( setup.IsConnectionStringUserConfigured );

			setup.WithConnectionString( sourceData.ConnectionString );
			ClassicAssert.IsTrue( setup.IsConnectionKeepAliveSecondsUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionRetryCountUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionRetryDelayMillisecondsUserConfigured );
			ClassicAssert.IsTrue( setup.IsConnectionStringUserConfigured );
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
