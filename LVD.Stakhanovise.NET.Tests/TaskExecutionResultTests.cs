using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using Bogus;

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
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsTrue( successful.ExecutedSuccessfully );
			Assert.IsFalse( successful.ExecutionCancelled );
			Assert.IsFalse( successful.ExecutionFailed );
			Assert.IsNull( successful.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				successful.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAtTicks, successful.RetryAtTicks );
			Assert.AreEqual( sampleFaultErrorThresholdCount, successful.FaultErrorThresholdCount );
		}

		[Test]
		public void Test_CanCreate_Cancelled ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo cancelledInfo = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration = faker.Date.Timespan();
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsFalse( cancelled.ExecutedSuccessfully );
			Assert.IsTrue( cancelled.ExecutionCancelled );
			Assert.IsFalse( cancelled.ExecutionFailed );
			Assert.IsNull( cancelled.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				cancelled.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAtTicks, cancelled.RetryAtTicks );
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
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			Assert.IsFalse( failedWithError.ExecutedSuccessfully );
			Assert.IsFalse( failedWithError.ExecutionCancelled );
			Assert.IsTrue( failedWithError.ExecutionFailed );
			Assert.NotNull( failedWithError.Error );
			Assert.AreEqual( failedWithErrorInfo.Error, failedWithError.Error );

			Assert.AreEqual( ( long )( Math.Ceiling( sampleDuration.TotalMilliseconds ) ),
				failedWithError.ProcessingTimeMilliseconds );

			Assert.AreEqual( sampleRetryAtTicks, failedWithError.RetryAtTicks );
			Assert.AreEqual( sampleFaultErrorThresholdCount, failedWithError.FaultErrorThresholdCount );
		}

		[Test]
		public void Test_CanCompare_ExpectedEquals_Successful ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo successfulInfo = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration = faker.Date.Timespan();
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful1 = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult successful2 = new TaskExecutionResult( successfulInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
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
			long sampleRetryAtTicks1 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TaskExecutionResultInfo successfulInfo2 = TaskExecutionResultInfo.Successful();
			TimeSpan sampleDuration2 = faker.Date.Timespan();
			long sampleRetryAtTicks2 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult successful1 = new TaskExecutionResult( successfulInfo1,
				duration: sampleDuration1,
				retryAtTicks: sampleRetryAtTicks1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult successful2 = new TaskExecutionResult( successfulInfo2,
				duration: sampleDuration2,
				retryAtTicks: sampleRetryAtTicks2,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount2 );

			Assert.AreNotEqual( successful1, successful2 );
		}

		[Test]
		public void Test_CanCompare_ExpectedEquals_Cancelled ()
		{
			Faker faker = new Faker();
			TaskExecutionResultInfo cancelledInfo = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration = faker.Date.Timespan();
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled1 = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult cancelled2 = new TaskExecutionResult( cancelledInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
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
			long sampleRetryAtTicks1 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TaskExecutionResultInfo cancelledInfo2 = TaskExecutionResultInfo.Cancelled();
			TimeSpan sampleDuration2 = faker.Date.Timespan();
			long sampleRetryAtTicks2 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult cancelled1 = new TaskExecutionResult( cancelledInfo1,
				duration: sampleDuration1,
				retryAtTicks: sampleRetryAtTicks1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult cancelled2 = new TaskExecutionResult( cancelledInfo2,
				duration: sampleDuration2,
				retryAtTicks: sampleRetryAtTicks2,
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
			long sampleRetryAtTicks = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError1 = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount );

			TaskExecutionResult failedWithError2 = new TaskExecutionResult( failedWithErrorInfo,
				duration: sampleDuration,
				retryAtTicks: sampleRetryAtTicks,
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
			long sampleRetryAtTicks1 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount1 = faker.Random.Int( 1, 5 );

			TimeSpan sampleDuration2 = faker.Date.Timespan();
			long sampleRetryAtTicks2 = faker.Random.Long( 1 );
			int sampleFaultErrorThresholdCount2 = faker.Random.Int( 1, 5 );

			TaskExecutionResult failedWithError1 = new TaskExecutionResult( failedWithErrorInfo1,
				duration: sampleDuration1,
				retryAtTicks: sampleRetryAtTicks1,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount1 );

			TaskExecutionResult failedWithError2 = new TaskExecutionResult( failedWithErrorInfo2,
				duration: sampleDuration2,
				retryAtTicks: sampleRetryAtTicks2,
				faultErrorThresholdCount: sampleFaultErrorThresholdCount2 );

			Assert.AreNotEqual( failedWithError1, failedWithError2 );
		}
	}
}
