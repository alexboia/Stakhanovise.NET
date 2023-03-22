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
using LVD.Stakhanovise.NET.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Common.Tests.HelpersTests
{
	[TestFixture]
	public class TaskExtensionsTests
	{
		[Test]
		public async Task Test_CanInvokeCleanup_TaskWithSuccessResult()
		{
			bool cleanupCalled = true;
			int expectedResult = new Faker().Random.Int();

			int result = await Task.Run( async () =>
			{
				await Task.Delay( 500 );
				return expectedResult;
			} ).WithCleanup( prev => cleanupCalled = true );

			Assert.AreEqual( expectedResult, result );
			Assert.IsTrue( cleanupCalled );
		}

		[Test]
		public void Test_CanInvokeCleanup_TaskFailedWithException()
		{
			Faker faker = null;
			bool cleanupCalled = false;

			Assert.ThrowsAsync<NullReferenceException>( async () => await Task
				.Run( () => faker.Random.Int() )
				.WithCleanup( prev => cleanupCalled = true ) );

			Assert.IsTrue( cleanupCalled );
		}

		[Test]
		public void Test_CanInvokeCleanup_CancelledTask()
		{
			Faker faker = new Faker();
			CancellationTokenSource cts = new CancellationTokenSource();
			CancellationToken token = cts.Token;
			bool cleanupCalled = false;

			cts.Cancel();

			Assert.CatchAsync<OperationCanceledException>( async () => await Task
				.Run( () =>
				 {
					 token.ThrowIfCancellationRequested();
					 return faker.Random.Int();
				 }, token )
				.WithCleanup( prev => cleanupCalled = true ) );

			Assert.IsTrue( cleanupCalled );
		}
	}
}
