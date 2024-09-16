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
using LVD.Stakhanovise.NET.Tests.Payloads;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Tests.ResultsTests
{
	[TestFixture]
	public class QueuedTaskResultTests
	{
		[Test]
		[Repeat( 5 )]
		public void Test_CanCreateFromQueuedTask ()
		{
			Faker<QueuedTask> taskFaker =
				GetQueuedTaskFaker();

			QueuedTask task = taskFaker
				.Generate();

			QueuedTaskResult result = new QueuedTaskResult( task );

			ClassicAssert.AreEqual( task.Id, result.Id );
			ClassicAssert.AreEqual( task.Type, result.Type );
			ClassicAssert.AreSame( task.Payload, result.Payload );
			ClassicAssert.AreEqual( task.Source, result.Source );
			ClassicAssert.AreEqual( task.PostedAtTs, result.PostedAtTs );
			ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			ClassicAssert.AreEqual( QueuedTaskStatus.Unprocessed, result.Status );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanUpdateFromExecutionResult_Successful ()
		{
			Faker faker = new Faker();
			DateTimeOffset now = DateTimeOffset
				.UtcNow;

			Faker<QueuedTask> taskFaker =
				GetQueuedTaskFaker();

			QueuedTask task = taskFaker
				.Generate();

			QueuedTaskResult result = new QueuedTaskResult( task );

			TaskExecutionResult successful = new TaskExecutionResult( TaskExecutionResultInfo.Successful(),
				duration: faker.Date.Timespan(),
				retryAt: faker.Date.FutureOffset(),
				faultErrorThresholdCount: faker.Random.Int( 1, 5 ) );

			QueuedTaskProduceInfo repostWithInfo = result.UdpateFromExecutionResult( successful );

			ClassicAssert.Null( repostWithInfo );
			ClassicAssert.IsNull( result.LastError );
			ClassicAssert.AreEqual( QueuedTaskStatus.Processed, result.Status );
			ClassicAssert.AreEqual( successful.ProcessingTimeMilliseconds, result.ProcessingTimeMilliseconds );
			ClassicAssert.GreaterOrEqual( result.ProcessingFinalizedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanUpdateFromExecutionResult_Cancelled ()
		{
			Faker faker = new Faker();
			DateTimeOffset now = DateTimeOffset
				.UtcNow;

			Faker<QueuedTask> taskFaker =
				GetQueuedTaskFaker();

			QueuedTask task = taskFaker
				.Generate();

			QueuedTaskResult result = new QueuedTaskResult( task );

			TaskExecutionResult cancelled = new TaskExecutionResult( TaskExecutionResultInfo.Cancelled(),
				duration: faker.Date.Timespan(),
				retryAt: faker.Date.FutureOffset(),
				faultErrorThresholdCount: faker.Random.Int( 1, 5 ) );

			QueuedTaskProduceInfo repostWithInfo = result.UdpateFromExecutionResult( cancelled );

			ClassicAssert.Null( repostWithInfo );
			ClassicAssert.IsNull( result.LastError );
			ClassicAssert.AreEqual( QueuedTaskStatus.Cancelled, result.Status );
			ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			ClassicAssert.GreaterOrEqual( result.ProcessingFinalizedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public void Test_CanUpdateFromExecutionResult_WithError_Recoverable ( int faultErrorThresholdCount )
		{
			Faker faker = new Faker();
			QueuedTaskProduceInfo repostWithInfo = null;

			DateTimeOffset now = DateTimeOffset
				.UtcNow;

			Faker<QueuedTask> taskFaker =
				GetQueuedTaskFaker();

			QueuedTask task = taskFaker
				.Generate();

			QueuedTaskResult result = new QueuedTaskResult( task );

			TaskExecutionResultInfo failedWithErrorInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( faker.System.Exception() ),
					isRecoverable: true );

			TaskExecutionResult failedWithError = new TaskExecutionResult( failedWithErrorInfo,
				duration: faker.Date.Timespan(),
				retryAt: faker.Date.FutureOffset(),
				faultErrorThresholdCount: faultErrorThresholdCount );

			//1 to faultErrorThresholdCount -> Error status
			for ( int i = 1; i <= faultErrorThresholdCount; i++ )
			{
				repostWithInfo = result.UdpateFromExecutionResult( failedWithError );
				ClassicAssert.NotNull( repostWithInfo );

				ClassicAssert.AreEqual( QueuedTaskStatus.Error, result.Status );
				ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
				ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
				ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
				ClassicAssert.AreEqual( failedWithErrorInfo.Error, result.LastError );
				ClassicAssert.AreEqual( i, result.ErrorCount );
			}

			//Antoher failure -> Faulted
			repostWithInfo = result.UdpateFromExecutionResult( failedWithError );
			ClassicAssert.NotNull( repostWithInfo );

			ClassicAssert.AreEqual( QueuedTaskStatus.Faulted, result.Status );
			ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
			ClassicAssert.AreEqual( failedWithErrorInfo.Error, result.LastError );
			ClassicAssert.AreEqual( faultErrorThresholdCount + 1, result.ErrorCount );

			//Antoher failure after that -> Fataled
			repostWithInfo = result.UdpateFromExecutionResult( failedWithError );
			ClassicAssert.Null( repostWithInfo );

			ClassicAssert.AreEqual( QueuedTaskStatus.Fatal, result.Status );
			ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
			ClassicAssert.AreEqual( failedWithErrorInfo.Error, result.LastError );
			ClassicAssert.AreEqual( faultErrorThresholdCount + 2, result.ErrorCount );	
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public void Test_CanUpdateFromExecutionResult_WithError_NotRecoverable ( int faultErrorThresholdCount )
		{
			Faker faker = new Faker();
			DateTimeOffset now = DateTimeOffset
				.UtcNow;

			Faker<QueuedTask> taskFaker =
				GetQueuedTaskFaker();

			QueuedTask task = taskFaker
				.Generate();

			QueuedTaskResult result = new QueuedTaskResult( task );

			TaskExecutionResultInfo failedWithErrorInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( faker.System.Exception() ),
					isRecoverable: false );

			TaskExecutionResult failedWithError = new TaskExecutionResult( failedWithErrorInfo,
				duration: faker.Date.Timespan(),
				retryAt: faker.Date.FutureOffset(),
				faultErrorThresholdCount: faultErrorThresholdCount );

			for ( int i = 1; i <= faultErrorThresholdCount + 2; i++ )
			{
				if ( i > 1 )
					Assert.Throws<InvalidOperationException>( () => result.UdpateFromExecutionResult( failedWithError ) );
				else
					ClassicAssert.IsNull( result.UdpateFromExecutionResult( failedWithError ) );

				ClassicAssert.AreEqual( 1, result.ErrorCount );
				ClassicAssert.AreEqual( failedWithErrorInfo.Error, result.LastError );
				ClassicAssert.IsFalse( result.LastErrorIsRecoverable );
				ClassicAssert.AreEqual( QueuedTaskStatus.Fatal, result.Status );
				ClassicAssert.AreEqual( 0, result.ProcessingTimeMilliseconds );
				ClassicAssert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
				ClassicAssert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
			}
		}

		private Faker<QueuedTask> GetQueuedTaskFaker ()
		{
			Faker<QueuedTask> qFaker = new Faker<QueuedTask>();

			qFaker.RuleFor( q => q.Id, f => Guid.NewGuid() );
			qFaker.RuleFor( q => q.LockedUntilTs, f => f.Date.FutureOffset() );
			qFaker.RuleFor( q => q.Payload, f => new SampleTaskPayload( f.Random.Int() ) );
			qFaker.RuleFor( q => q.Type, f => typeof( SampleTaskPayload ).FullName );
			qFaker.RuleFor( q => q.Source, f => nameof( GetQueuedTaskFaker ) );
			qFaker.RuleFor( q => q.PostedAtTs, f => f.Date.SoonOffset() );
			qFaker.RuleFor( q => q.Priority, f => f.Random.Int( 1, 1000 ) );

			return qFaker;
		}
	}
}
