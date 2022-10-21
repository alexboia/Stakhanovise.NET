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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class ResultQueueProcessingRequestTests : BaseAsyncProcessingRequestTests
	{
		[Test]
		[TestCase( 0 )]
		[TestCase( 1 )]
		[TestCase( 5 )]
		public void Test_CanSetCompleted_SingleThread ( int expectedResult )
		{
			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			RunTest_CanSetCompleted_SingleThread<int>( () => new ResultQueueProcessingRequest( 1,
					resultToUpdate: resultMock.Object,
					timeoutMilliseconds: 0,
					maxFailCount: 3 ),
				expectedResult,
				expectSame: false );
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
			Faker faker =
				new Faker();

			int expectedResult = faker.Random
				.Int( 0, 10 );

			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			await RunTest_CanSetCompleted_MultiThread<int>( () => new ResultQueueProcessingRequest( 1,
					resultToUpdate: resultMock.Object,
					timeoutMilliseconds: 0,
					maxFailCount: 3 ),
				syncOnCheckpoints,
				nThreads,
				expectedResult,
				expectSame: false );
		}

		[Test]
		public void Test_CanSetCancelledManually_SingleThread ()
		{
			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			RunTest_CanSetCancelledManually_SingleThread<int>( () => new ResultQueueProcessingRequest( 1,
				 resultToUpdate: resultMock.Object,
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
			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			await RunTest_CanSetCancelledManually_MultiThread<int>( () => new ResultQueueProcessingRequest( 1,
					resultToUpdate: resultMock.Object,
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
			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			RunTest_CanCancelItselfViaTimeout<int>( () => new ResultQueueProcessingRequest( 1,
				 resultToUpdate: resultMock.Object,
				 timeoutMilliseconds: timeoutMilliseconds,
				 maxFailCount: 3 ) );
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanSetFailed_SingleThread ( int maxFailCount )
		{
			Mock<IQueuedTaskResult> resultMock =
				new Mock<IQueuedTaskResult>();

			RunTest_CanSetFailed_SingleThread<int>( () => new ResultQueueProcessingRequest( 1,
					resultToUpdate: resultMock.Object,
					timeoutMilliseconds: 0,
					maxFailCount: maxFailCount ),
				maxFailCount );
		}
	}
}
