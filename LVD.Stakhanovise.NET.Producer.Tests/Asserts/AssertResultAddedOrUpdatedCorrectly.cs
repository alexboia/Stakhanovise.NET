using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Producer.Tests.Asserts
{
	public class AssertResultAddedOrUpdatedCorrectly
	{
		private PostgreSqlTaskQueueDataSource mDataSource;

		private AssertResultAddedOrUpdatedCorrectly( PostgreSqlTaskQueueDataSource dataSource )
		{
			mDataSource = dataSource;
		}

		public static AssertResultAddedOrUpdatedCorrectly LookIn( PostgreSqlTaskQueueDataSource dataSource )
		{
			return new AssertResultAddedOrUpdatedCorrectly( dataSource );
		}

		public async Task CheckAsync( IQueuedTask queuedTask )
		{
			Assert.NotNull( queuedTask );

			IQueuedTaskResult queuedTaskResult =
				await GetQueuedTaskResultFromDbByIdAsync( queuedTask );

			Assert.NotNull( queuedTaskResult );
			Assert.NotNull( queuedTaskResult.Payload );

			Assert.AreEqual( queuedTask.Id,
				queuedTaskResult.Id );
			Assert.AreEqual( queuedTask.Type,
				queuedTaskResult.Type );
			Assert.AreEqual( queuedTask.Source,
				queuedTaskResult.Source );
			Assert.AreEqual( queuedTask.Priority,
				queuedTaskResult.Priority );
			Assert.LessOrEqual( Math.Abs( ( queuedTask.PostedAtTs - queuedTaskResult.PostedAtTs ).TotalMilliseconds ),
				10 );
			Assert.AreEqual( QueuedTaskStatus.Unprocessed,
				queuedTaskResult.Status );
		}

		private async Task<IQueuedTaskResult> GetQueuedTaskResultFromDbByIdAsync( IQueuedTask queuedTask )
		{
			return await mDataSource.GetQueuedTaskResultFromDbByIdAsync( queuedTask.Id );
		}
	}
}
