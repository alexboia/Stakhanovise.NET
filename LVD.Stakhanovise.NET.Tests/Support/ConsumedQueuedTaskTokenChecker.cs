using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ConsumedQueuedTaskTokenChecker : IDisposable
	{
		private IQueuedTaskToken mPreviousTaskToken = null;

		private List<IQueuedTaskToken> mDequeuedTokens =
			new List<IQueuedTaskToken>();

		public void AssertConsumedTokenValid ( IQueuedTaskToken newTaskToken, AbstractTimestamp now )
		{
			Assert.NotNull( newTaskToken );
			Assert.NotNull( newTaskToken.DequeuedAt );
			Assert.NotNull( newTaskToken.DequeuedTask );
			Assert.NotNull( newTaskToken.LastQueuedTaskResult );

			Assert.AreEqual( now, newTaskToken.DequeuedAt );

			Assert.IsFalse( mDequeuedTokens.Any( t => t.DequeuedTask.Id == newTaskToken.DequeuedTask.Id ) );

			if ( mPreviousTaskToken != null )
				Assert.GreaterOrEqual( newTaskToken.DequeuedTask.PostedAt, mPreviousTaskToken.DequeuedTask.PostedAt );

			mPreviousTaskToken = newTaskToken;
			mDequeuedTokens.Add( newTaskToken );
		}

		public void Dispose ()
		{
			mPreviousTaskToken = null;
			mDequeuedTokens.Clear();
		}
	}
}
