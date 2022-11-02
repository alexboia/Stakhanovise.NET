using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertQueuedTaskResultMatchesExpectedResult
	{
		private IQueuedTaskResult mExpectedResult;

		private AssertQueuedTaskResultMatchesExpectedResult( IQueuedTaskResult expectedResult )
		{
			mExpectedResult = expectedResult
				?? throw new ArgumentNullException( nameof( expectedResult ) );
		}

		public static AssertQueuedTaskResultMatchesExpectedResult For( IQueuedTaskResult expectedResult )
		{
			return new AssertQueuedTaskResultMatchesExpectedResult( expectedResult );
		}

		public void Check( IQueuedTaskResult actualResult )
		{
			Assert.NotNull( actualResult );

			Assert.AreEqual( actualResult.ErrorCount,
				mExpectedResult.ErrorCount );

			mExpectedResult.PostedAtTs
				.AssertEquals( actualResult.PostedAtTs, 10 );

			mExpectedResult.FirstProcessingAttemptedAtTs
				.AssertEquals( actualResult.FirstProcessingAttemptedAtTs, 10 );
			mExpectedResult.LastProcessingAttemptedAtTs
				.AssertEquals( actualResult.LastProcessingAttemptedAtTs, 10 );
			mExpectedResult.ProcessingFinalizedAtTs
				.AssertEquals( actualResult.ProcessingFinalizedAtTs, 10 );

			Assert.AreEqual( actualResult.LastError,
				mExpectedResult.LastError );
			Assert.AreEqual( actualResult.LastErrorIsRecoverable,
				mExpectedResult.LastErrorIsRecoverable );

			Assert.AreEqual( actualResult.Priority,
				mExpectedResult.Priority );

			Assert.AreEqual( actualResult.ProcessingTimeMilliseconds,
				mExpectedResult.ProcessingTimeMilliseconds );
			Assert.AreEqual( actualResult.Source,
				mExpectedResult.Source );
			Assert.AreEqual( actualResult.Status,
				mExpectedResult.Status );
			Assert.AreEqual( actualResult.Type,
				mExpectedResult.Type );
		}
	}
}
