using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
			ClassicAssert.NotNull( actualResult );

			ClassicAssert.AreEqual( actualResult.ErrorCount,
				mExpectedResult.ErrorCount );

			mExpectedResult.PostedAtTs
				.AssertEquals( actualResult.PostedAtTs, 10 );

			mExpectedResult.FirstProcessingAttemptedAtTs
				.AssertEquals( actualResult.FirstProcessingAttemptedAtTs, 10 );
			mExpectedResult.LastProcessingAttemptedAtTs
				.AssertEquals( actualResult.LastProcessingAttemptedAtTs, 10 );
			mExpectedResult.ProcessingFinalizedAtTs
				.AssertEquals( actualResult.ProcessingFinalizedAtTs, 10 );

			ClassicAssert.AreEqual( actualResult.LastError,
				mExpectedResult.LastError );
			ClassicAssert.AreEqual( actualResult.LastErrorIsRecoverable,
				mExpectedResult.LastErrorIsRecoverable );

			ClassicAssert.AreEqual( actualResult.Priority,
				mExpectedResult.Priority );

			ClassicAssert.AreEqual( actualResult.ProcessingTimeMilliseconds,
				mExpectedResult.ProcessingTimeMilliseconds );
			ClassicAssert.AreEqual( actualResult.Source,
				mExpectedResult.Source );
			ClassicAssert.AreEqual( actualResult.Status,
				mExpectedResult.Status );
			ClassicAssert.AreEqual( actualResult.Type,
				mExpectedResult.Type );
		}
	}
}
