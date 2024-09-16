// 
// BSD 3-Clause License
// 
// Copyright (c) 2020 - 2023, Boia Alexandru
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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Tests;
using Npgsql;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Data;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Common.Tests.HelpersTests
{
	[TestFixture]
	public class NpgsqlConnectionExtensionsTests : BaseTestWithConfiguration
	{
		[Test]
		public void Test_TryOpenConnectionAsync_ConnectionOptions_Null_ThrowsArgumentNullException()
		{
			ConnectionOptions options = null;
			Assert.ThrowsAsync<ArgumentNullException>( async () => await options.TryOpenConnectionAsync() );
		}

		[Test]
		public void Test_TryOpenConnectionAsync_String_Null_ThrowsArgumentNullException()
		{
			string connectionString = null;
			Assert.ThrowsAsync<ArgumentNullException>( async () => await connectionString.TryOpenConnectionAsync() );
		}

		[Test]
		public void Test_TryOpenConnectionAsync_MaxRetryCount_LessThanOne_ThrowsArgumentOutOfRangeException()
		{
			string connectionString = "server=localhost;database=mydatabase;user id=myuser;password=mypassword";
			Assert.ThrowsAsync<ArgumentOutOfRangeException>( async () => await connectionString.TryOpenConnectionAsync( maxRetryCount: 0 ) );
		}

		[Test]
		public void Test_TryOpenConnectionAsync_RetryDelayMilliseconds_LessThanOne_ThrowsArgumentOutOfRangeException()
		{
			string connectionString = "server=localhost;database=mydatabase;user id=myuser;password=mypassword";
			Assert.ThrowsAsync<ArgumentOutOfRangeException>( async () => await connectionString.TryOpenConnectionAsync( retryDelayMilliseconds: 0 ) );
		}

		[Test]
		public async Task Test_OpenConnectionAsync_ValidConnectionString_OpensSuccessful()
		{
			string connectionString = GetConnectionString( "testDbConnectionString" );
			NpgsqlConnection conn = await connectionString.TryOpenConnectionAsync();
			ClassicAssert.IsNotNull( conn );
			ClassicAssert.AreEqual( conn.State, ConnectionState.Open );
			conn.Close();
		}

		[Test]
		[TestCase( 1, 100 )]
		[TestCase( 1, 200 )]
		[TestCase( 1, 500 )]

		[TestCase( 3, 100 )]
		[TestCase( 3, 200 )]
		[TestCase( 3, 500 )]

		[TestCase( 5, 100 )]
		[TestCase( 5, 200 )]
		[TestCase( 5, 500 )]
		public async Task Test_OpenConnectionAsync_InvvalidConnectionString_OpenFails_NoCancellation( int maxRetryCount, int retryDelayMilliseconds )
		{
			string connectionString = "server=localhost;database=mydatabase;user id=myuser;password=mypassword";
			NpgsqlConnection conn = await connectionString.TryOpenConnectionAsync( maxRetryCount, retryDelayMilliseconds );
			ClassicAssert.IsNull( conn );
		}
	}
}
