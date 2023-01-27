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
using System;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class TaskExecutionResultTests
	{
		[Test]
		public void Test_CanCreate_Successful ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo successfulInfo = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsTrue( successful.ExecutedSuccessfully );
			Assert.IsFalse( successful.ExecutionCancelled );
			Assert.IsFalse( successful.ExecutionFailed );
			Assert.IsNull( successful.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				successful.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAt, successful.RetryAt );
			Assert.AreEqual( sampleFaultErrorThresholdCount, successful.FaultErrorThresholdCount );
		}

		[Test]
		public void Test_CanCreate_Cancelled ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo cancelledInfo = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsFalse( cancelled.ExecutedSuccessfully );
			Assert.IsTrue( cancelled.ExecutionCancelled );
			Assert.IsFalse( cancelled.ExecutionFailed );
			Assert.IsNull( cancelled.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				cancelled.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAt, cancelled.RetryAt );
			Assert.AreEqual( sampleFaultErrorThresholdCount, cancelled.FaultErrorThresholdCount );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_CanCreate_WithError ( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithErrorInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsFalse( failedWithError.ExecutedSuccessfully );
			Assert.IsFalse( failedWithError.ExecutionCancelled );
			Assert.IsTrue( failedWithError.ExecutionFailed );
			Assert.NotNull( failedWithError.Error );
			Assert.AreEqual( failedWithErrorInfo.Error, failedWithError.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				failedWithError.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAt, failedWithError.RetryAt );
			Assert.AreEqual( sampleFaultErrorThresholdCount, failedWithError.FaultErrorThresholdCount );
		}

		[Test]
		public void Test_CanCompare_ExpectedEquals_Successful ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo successfulInfo = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful1 = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult successful2 = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.AreEqual( successful1, successful2 );
			Assert.AreEqual( successful1, successful1 );
		}

		[Test]
		public void Test_CanCompare_ExpectedNotEqual_Successful ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo successfulInfo1 = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration1 = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt1 = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TaskExecutionResultInfo successfulInfo2 = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration2 = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt2 = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful1 = new TaskExecutionResult( successfulInfo1,
				duration: sampleDuration1,
				retryAt: sampleRetryAt1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult successful2 = new TaskExecutionResult( successfulInfo2,
				duration: sampleDuration2,
				retryAt: sampleRetryAt2,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount2 );

			Assert.AreNotEqual( successful1, successful2 );
		}

		[Test]
		public void Test_CanCompare_ExpectedEquals_Cancelled ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo cancelledInfo = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled1 = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult cancelled2 = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.AreEqual( cancelled1, cancelled2 );
			Assert.AreEqual( cancelled1, cancelled1 );
		}

		[Test]
		public void Test_CanCompare_ExpectedNotEqual_Cancelled ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo cancelledInfo1 = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration1 = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt1 = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TaskExecutionResultInfo cancelledInfo2 = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration2 = faker.Date.Timespan();
			DateTimeOffset sampleRetry2 = faker.Date.PastOffset();
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled1 = new TaskExecutionResult( cancelledInfo1,
				duration: sampleDuration1,
				retryAt: sampleRetryAt1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult cancelled2 = new TaskExecutionResult( cancelledInfo2,
				duration: sampleDuration2,
				retryAt: sampleRetry2,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount2 );

			Assert.AreNotEqual( cancelled1, cancelled2 );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_CanCompare_ExpectedEquals_WithError ( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithErrorInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			TimeSpan sampleDuration = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt = faker.Date.RecentOffset();
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError1 = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult failedWithError2 = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAt: sampleRetryAt,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.AreEqual( failedWithError1, failedWithError2 );
			Assert.AreEqual( failedWithError1, failedWithError1 );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_CanCompare_ExpectedNotEqual_WithError ( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithErrorInfo1 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			TaskExecutionResultInfo failedWithErrorInfo2 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			TimeSpan sampleDuration1 = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt1 = faker.Date.SoonOffset();
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TimeSpan sampleDuration2 = faker.Date.Timespan();
			DateTimeOffset sampleRetryAt2 = faker.Date.SoonOffset();
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError1 = new TaskExecutionResult( failedWithErrorInfo1,
				duration: sampleDuration1,
				retryAt: sampleRetryAt1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult failedWithError2 = new TaskExecutionResult( failedWithErrorInfo2,
				duration: sampleDuration2,
				retryAt: sampleRetryAt2,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount2 );

			Assert.AreNotEqual( failedWithError1, failedWithError2 );
		}
	}
}
