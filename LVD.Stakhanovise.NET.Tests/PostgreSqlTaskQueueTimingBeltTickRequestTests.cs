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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueTimingBeltTickRequestTests : BaseAsyncProcessingRequestTests
	{
		[Test]
		public void Test_CanSetCompleted_SingleThread ()
		{
			AbstractTimestamp timestamp =
				new AbstractTimestamp( 10, 1000 );

			RunTest_CanSetCompleted_SingleThread<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
					completionToken: tcs,
					timeoutMilliseconds: 0,
					maxFailCount: 3 ),
				timestamp,
				expectSame: true );
		}

		[Test]
		[TestCase( true, 2 )]
		[TestCase( true, 5 )]
		[TestCase( true, 10 )]

		[TestCase( false, 2 )]
		[TestCase( false, 5 )]
		[TestCase( false, 10 )]
		public async Task Test_CanSetCompleted_MultiThread ( bool syncOnCheckpoints, int nThreads )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();

			AbstractTimestamp timestamp =
				new AbstractTimestamp( 10, 1000 );

			await RunTest_CanSetCompleted_MultiThread<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
					completionToken: tcs,
					timeoutMilliseconds: 0,
					maxFailCount: 3 ),
				syncOnCheckpoints,
				nThreads,
				timestamp,
				expectSame: true );
		}

		[Test]
		public void Test_CanSetCancelledManually_SingleThread ()
		{
			RunTest_CanSetCancelledManually_SingleThread<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				 completionToken: tcs,
				 timeoutMilliseconds: 0,
				 maxFailCount: 3 ) );
		}

		[Test]
		[TestCase( true, 2 )]
		[TestCase( true, 5 )]
		[TestCase( true, 10 )]

		[TestCase( false, 2 )]
		[TestCase( false, 5 )]
		[TestCase( false, 10 )]
		public async Task Test_CanSetCancelledManually_MultiThread ( bool syncOnCheckpoints, int nThreads )
		{
			await RunTest_CanSetCancelledManually_MultiThread<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
					completionToken: tcs,
					timeoutMilliseconds: 0,
					maxFailCount: 3 ),
				syncOnCheckpoints: syncOnCheckpoints,
				nThreads: nThreads );
		}

		[Test]
		[TestCase( 100 )]
		[TestCase( 500 )]
		[TestCase( 1000 )]
		public void Test_CanCancelItselfViaTimeout ( int timeoutMilliseconds )
		{
			RunTest_CanCancelItselfViaTimeout<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: timeoutMilliseconds,
				maxFailCount: 3 ) );
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanSetFailed_SingleThread ( int maxFailCount )
		{
			RunTest_CanSetFailed_SingleThread<AbstractTimestamp>( tcs => new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
					completionToken: tcs,
					timeoutMilliseconds: 0,
					maxFailCount: maxFailCount ),
				maxFailCount );
		}
	}
}
