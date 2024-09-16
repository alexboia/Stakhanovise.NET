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
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Tests.ModelTests
{
	[TestFixture]
	public class TaskExecutionResultInfoTests
	{
		[Test]
		public void Test_CanConstruct_Successful()
		{
			TaskExecutionResultInfo successful = TaskExecutionResultInfo.Successful();

			ClassicAssert.NotNull( successful );
			ClassicAssert.IsTrue( successful.ExecutedSuccessfully );
			ClassicAssert.IsFalse( successful.ExecutionCancelled );
			ClassicAssert.IsFalse( successful.ExecutionFailed );
			ClassicAssert.IsNull( successful.Error );
		}

		[Test]
		public void Test_CanConstruct_Cancelled()
		{
			TaskExecutionResultInfo cancelled = TaskExecutionResultInfo.Cancelled();

			ClassicAssert.NotNull( cancelled );
			ClassicAssert.IsFalse( cancelled.ExecutedSuccessfully );
			ClassicAssert.IsTrue( cancelled.ExecutionCancelled );
			ClassicAssert.IsFalse( cancelled.ExecutionFailed );
			ClassicAssert.IsNull( cancelled.Error );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		[Repeat( 10 )]
		public void Test_CanConstruct_WithError( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			ClassicAssert.NotNull( failedWithError );
			ClassicAssert.IsFalse( failedWithError.ExecutedSuccessfully );
			ClassicAssert.IsFalse( failedWithError.ExecutionCancelled );
			ClassicAssert.IsTrue( failedWithError.ExecutionFailed );

			ClassicAssert.NotNull( failedWithError.Error );
			ClassicAssert.AreEqual( isRecoverable, failedWithError.IsRecoverable );
		}

		[Test]
		public void Test_CanCompare_Successful()
		{
			TaskExecutionResultInfo successful1 = TaskExecutionResultInfo.Successful();
			TaskExecutionResultInfo successful2 = TaskExecutionResultInfo.Successful();

			ClassicAssert.AreEqual( successful1, successful2 );
			ClassicAssert.AreEqual( successful1, successful1 );

			ClassicAssert.AreNotSame( successful1, successful2 );
		}

		[Test]
		public void Test_CanCompare_Cancelled()
		{
			TaskExecutionResultInfo cancelled1 = TaskExecutionResultInfo.Cancelled();
			TaskExecutionResultInfo cancelled2 = TaskExecutionResultInfo.Cancelled();

			ClassicAssert.AreEqual( cancelled1, cancelled2 );
			ClassicAssert.AreEqual( cancelled1, cancelled1 );

			ClassicAssert.AreNotSame( cancelled1, cancelled2 );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		[Repeat( 10 )]
		public void Test_CanCompare_WithError_ExpectedEqual( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError1 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );
			TaskExecutionResultInfo failedWithError2 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			ClassicAssert.AreEqual( failedWithError1, failedWithError2 );
			ClassicAssert.AreEqual( failedWithError1, failedWithError1 );

			ClassicAssert.AreNotSame( failedWithError1, failedWithError2 );
		}

		[Test]
		public void Test_CanCompare_WithError_ExpectedNotEqual()
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError1 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), true );
			TaskExecutionResultInfo failedWithError2 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), false );

			ClassicAssert.AreNotEqual( failedWithError1, failedWithError2 );

			TaskExecutionResultInfo failedWithError3 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( "t1", "m1", "s1" ), true );
			TaskExecutionResultInfo failedWithError4 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( "t2", "m2", "s2" ), true );

			ClassicAssert.AreNotEqual( failedWithError3, failedWithError4 );
		}
	}
}
