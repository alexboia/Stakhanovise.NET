using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertTaskTokensListMatchesExpectedCount
	{
		private int mExpectedCount;

		private AssertTaskTokensListMatchesExpectedCount( int expectedCount )
		{
			mExpectedCount = expectedCount;
		}

		public static AssertTaskTokensListMatchesExpectedCount For( int expectedCount )
		{
			return new AssertTaskTokensListMatchesExpectedCount( expectedCount );
		}

		public void Check( IEnumerable<IQueuedTaskToken> taskTokenList )
		{
			ClassicAssert.AreEqual( mExpectedCount,
				taskTokenList.Count() );

			foreach ( IQueuedTaskToken token in taskTokenList )
				ClassicAssert.AreEqual( 1, taskTokenList.Count( t => t.DequeuedTask.Id == token.DequeuedTask.Id ) );
		}
	}
}
