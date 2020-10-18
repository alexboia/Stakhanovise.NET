using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ConsumedQueuedTaskTokenChecker : IDisposable
	{
		private IQueuedTaskToken mPreviousTaskToken = null;

		private PostgreSqlTaskQueueDataSource mDataSource = null;

		private List<IQueuedTaskToken> mDequeuedTokens =
			new List<IQueuedTaskToken>();

		public ConsumedQueuedTaskTokenChecker( PostgreSqlTaskQueueDataSource dataSource)
		{
			mDataSource = dataSource;
		}

		public void AssertConsumedTokenValid ( IQueuedTaskToken newTaskToken, DateTimeOffset now )
		{
			Assert.NotNull( newTaskToken );
			Assert.NotNull( newTaskToken.DequeuedAt );
			Assert.NotNull( newTaskToken.DequeuedTask );
			Assert.NotNull( newTaskToken.LastQueuedTaskResult );

			Assert.AreEqual( now, newTaskToken.DequeuedAt );

			Assert.IsFalse( mDequeuedTokens.Any( t => t.DequeuedTask.Id == newTaskToken.DequeuedTask.Id ) );

			if ( mPreviousTaskToken != null )
				Assert.GreaterOrEqual( newTaskToken.DequeuedTask.PostedAtTs, mPreviousTaskToken.DequeuedTask.PostedAtTs );

			mPreviousTaskToken = newTaskToken;
			mDequeuedTokens.Add( newTaskToken );
		}

		public async Task AssertTaskNotInDbAnymoreAsync ( IQueuedTaskToken newTaskToken )
		{
			Assert.IsNull( await mDataSource.GetQueuedTaskFromDbByIdAsync( newTaskToken
				.DequeuedTask
				.Id ) );
		}

		public async Task AssertTaskResultInDbAndCorrectAsync ( IQueuedTaskToken newTaskToken )
		{
			QueuedTaskResult dbResult = await mDataSource.GetQueuedTaskResultFromDbByIdAsync( newTaskToken
				.DequeuedTask
				.Id );

			dbResult.AssertMatchesResult( newTaskToken
				.LastQueuedTaskResult );
		}

		public void Dispose ()
		{
			mPreviousTaskToken = null;
			mDequeuedTokens.Clear();
		}
	}
}
