﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using Bogus;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System.Runtime.InteropServices.ComTypes;

namespace LVD.Stakhanovise.NET.Tests
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

			Assert.AreEqual( task.Id, result.Id );
			Assert.AreEqual( task.Type, result.Type );
			Assert.AreSame( task.Payload, result.Payload );
			Assert.AreEqual( task.Source, result.Source );
			Assert.AreEqual( task.PostedAtTs, result.PostedAtTs );
			Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			Assert.AreEqual( QueuedTaskStatus.Unprocessed, result.Status );
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

			QueuedTaskInfo repostWithInfo = result.UdpateFromExecutionResult( successful );

			Assert.Null( repostWithInfo );
			Assert.IsNull( result.LastError );
			Assert.AreEqual( QueuedTaskStatus.Processed, result.Status );
			Assert.AreEqual( successful.ProcessingTimeMilliseconds, result.ProcessingTimeMilliseconds );
			Assert.GreaterOrEqual( result.ProcessingFinalizedAtTs, now );
			Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
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

			QueuedTaskInfo repostWithInfo = result.UdpateFromExecutionResult( cancelled );

			Assert.Null( repostWithInfo );
			Assert.IsNull( result.LastError );
			Assert.AreEqual( QueuedTaskStatus.Cancelled, result.Status );
			Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			Assert.GreaterOrEqual( result.ProcessingFinalizedAtTs, now );
			Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		public void Test_CanUpdateFromExecutionResult_WithError_Recoverable ( int faultErrorThresholdCount )
		{
			Faker faker = new Faker();
			QueuedTaskInfo repostWithInfo = null;

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
				Assert.NotNull( repostWithInfo );

				Assert.AreEqual( QueuedTaskStatus.Error, result.Status );
				Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
				Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
				Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
				Assert.AreEqual( failedWithErrorInfo.Error, result.LastError );
				Assert.AreEqual( i, result.ErrorCount );
			}

			//Antoher failure -> Faulted
			repostWithInfo = result.UdpateFromExecutionResult( failedWithError );
			Assert.NotNull( repostWithInfo );

			Assert.AreEqual( QueuedTaskStatus.Faulted, result.Status );
			Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
			Assert.AreEqual( failedWithErrorInfo.Error, result.LastError );
			Assert.AreEqual( faultErrorThresholdCount + 1, result.ErrorCount );

			//Antoher failure after that -> Fataled
			repostWithInfo = result.UdpateFromExecutionResult( failedWithError );
			Assert.Null( repostWithInfo );

			Assert.AreEqual( QueuedTaskStatus.Fatal, result.Status );
			Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
			Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
			Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
			Assert.AreEqual( failedWithErrorInfo.Error, result.LastError );
			Assert.AreEqual( faultErrorThresholdCount + 2, result.ErrorCount );	
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
					Assert.IsNull( result.UdpateFromExecutionResult( failedWithError ) );

				Assert.AreEqual( 1, result.ErrorCount );
				Assert.AreEqual( failedWithErrorInfo.Error, result.LastError );
				Assert.IsFalse( result.LastErrorIsRecoverable );
				Assert.AreEqual( QueuedTaskStatus.Fatal, result.Status );
				Assert.AreEqual( 0, result.ProcessingTimeMilliseconds );
				Assert.GreaterOrEqual( result.FirstProcessingAttemptedAtTs, now );
				Assert.GreaterOrEqual( result.LastProcessingAttemptedAtTs, now );
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