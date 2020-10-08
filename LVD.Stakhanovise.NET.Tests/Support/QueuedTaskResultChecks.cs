using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class QueuedTaskResultChecks
	{
		public static void AssertMatchesResult(this IQueuedTaskResult actualResult, IQueuedTaskResult expectedResult )
		{
			Assert.NotNull( actualResult );

			Assert.AreEqual( actualResult.ErrorCount,
				expectedResult.ErrorCount );
			Assert.AreEqual( actualResult.FirstProcessingAttemptedAtTs,
				expectedResult.FirstProcessingAttemptedAtTs );
			Assert.AreEqual( actualResult.LastProcessingAttemptedAtTs,
				expectedResult.LastProcessingAttemptedAtTs );
			Assert.AreEqual( actualResult.LastError,
				expectedResult.LastError );
			Assert.AreEqual( actualResult.LastErrorIsRecoverable,
				expectedResult.LastErrorIsRecoverable );
			Assert.AreEqual( actualResult.PostedAt,
				expectedResult.PostedAt );
			Assert.AreEqual( actualResult.PostedAtTs,
				expectedResult.PostedAtTs );
			Assert.AreEqual( actualResult.Priority,
				expectedResult.Priority );
			Assert.AreEqual( actualResult.ProcessingFinalizedAtTs,
				expectedResult.ProcessingFinalizedAtTs );
			Assert.AreEqual( actualResult.ProcessingTimeMilliseconds,
				expectedResult.ProcessingTimeMilliseconds );
			Assert.AreEqual( actualResult.Source,
				expectedResult.Source );
			Assert.AreEqual( actualResult.Status,
				expectedResult.Status );
			Assert.AreEqual( actualResult.Type,
				expectedResult.Type );
		}
	}
}
