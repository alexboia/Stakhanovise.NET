using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class TaskExecutionResultInfoTests
	{
		[Test]
		public void Test_CanConstruct_Successful ()
		{
			TaskExecutionResultInfo successful = TaskExecutionResultInfo.Successful();

			Assert.NotNull( successful );
			Assert.IsTrue( successful.ExecutedSuccessfully );
			Assert.IsFalse( successful.ExecutionCancelled );
			Assert.IsFalse( successful.ExecutionFailed );
			Assert.IsNull( successful.Error );
		}

		[Test]
		public void Test_CanConstruct_Cancelled ()
		{
			TaskExecutionResultInfo cancelled = TaskExecutionResultInfo.Cancelled();

			Assert.NotNull( cancelled );
			Assert.IsFalse( cancelled.ExecutedSuccessfully );
			Assert.IsTrue( cancelled.ExecutionCancelled );
			Assert.IsFalse( cancelled.ExecutionFailed );
			Assert.IsNull( cancelled.Error );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		[Repeat( 10 )]
		public void Test_CanConstruct_WithError ( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			Assert.NotNull( failedWithError );
			Assert.IsFalse( failedWithError.ExecutedSuccessfully );
			Assert.IsFalse( failedWithError.ExecutionCancelled );
			Assert.IsTrue( failedWithError.ExecutionFailed );

			Assert.NotNull( failedWithError.Error );
			Assert.AreEqual( isRecoverable, failedWithError.IsRecoverable );
		}

		[Test]
		public void Test_CanCompare_Successful ()
		{
			TaskExecutionResultInfo successful1 = TaskExecutionResultInfo.Successful();
			TaskExecutionResultInfo successful2 = TaskExecutionResultInfo.Successful();

			Assert.AreEqual( successful1, successful2 );
			Assert.AreEqual( successful1, successful1 );

			Assert.AreNotSame( successful1, successful2 );
		}

		[Test]
		public void Test_CanCompare_Cancelled ()
		{
			TaskExecutionResultInfo cancelled1 = TaskExecutionResultInfo.Cancelled();
			TaskExecutionResultInfo cancelled2 = TaskExecutionResultInfo.Cancelled();

			Assert.AreEqual( cancelled1, cancelled2 );
			Assert.AreEqual( cancelled1, cancelled1 );

			Assert.AreNotSame( cancelled1, cancelled2 );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		[Repeat( 10 )]
		public void Test_CanCompare_WithError_ExpectedEqual ( bool isRecoverable )
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError1 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );
			TaskExecutionResultInfo failedWithError2 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), isRecoverable );

			Assert.AreEqual( failedWithError1, failedWithError2 );
			Assert.AreEqual( failedWithError1, failedWithError1 );

			Assert.AreNotSame( failedWithError1, failedWithError2 );
		}

		[Test]
		public void Test_CanCompare_WithError_ExpectedNotEqual ()
		{
			Faker faker = new Faker();
			Exception exc = faker.System.Exception();
			TaskExecutionResultInfo failedWithError1 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), true );
			TaskExecutionResultInfo failedWithError2 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( exc ), false );

			Assert.AreNotEqual( failedWithError1, failedWithError2 );

			TaskExecutionResultInfo failedWithError3 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( "t1", "m1", "s1" ), true );
			TaskExecutionResultInfo failedWithError4 = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( "t2", "m2", "s2" ), true );

			Assert.AreNotEqual( failedWithError3, failedWithError4 );
		}
	}
}
