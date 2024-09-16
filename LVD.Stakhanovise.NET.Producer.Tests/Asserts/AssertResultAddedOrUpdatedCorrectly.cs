using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Producer.Tests.Asserts
{
	public class AssertResultAddedOrUpdatedCorrectly
	{
		private TaskQueueDataSource mDataSource;

		private AssertResultAddedOrUpdatedCorrectly( TaskQueueDataSource dataSource )
		{
			mDataSource = dataSource;
		}

		public static AssertResultAddedOrUpdatedCorrectly LookIn( TaskQueueDataSource dataSource )
		{
			return new AssertResultAddedOrUpdatedCorrectly( dataSource );
		}

		public async Task CheckAsync( IQueuedTask queuedTask )
		{
			ClassicAssert.NotNull( queuedTask );

			IQueuedTaskResult queuedTaskResult =
				await GetQueuedTaskResultFromDbByIdAsync( queuedTask );

			ClassicAssert.NotNull( queuedTaskResult );
			ClassicAssert.NotNull( queuedTaskResult.Payload );

			ClassicAssert.AreEqual( queuedTask.Id,
				queuedTaskResult.Id );
			ClassicAssert.AreEqual( queuedTask.Type,
				queuedTaskResult.Type );
			ClassicAssert.AreEqual( queuedTask.Source,
				queuedTaskResult.Source );
			ClassicAssert.AreEqual( queuedTask.Priority,
				queuedTaskResult.Priority );
			ClassicAssert.LessOrEqual( Math.Abs( ( queuedTask.PostedAtTs - queuedTaskResult.PostedAtTs ).TotalMilliseconds ),
				10 );
			ClassicAssert.AreEqual( QueuedTaskStatus.Unprocessed,
				queuedTaskResult.Status );
		}

		private async Task<IQueuedTaskResult> GetQueuedTaskResultFromDbByIdAsync( IQueuedTask queuedTask )
		{
			return await mDataSource.GetQueuedTaskResultFromDbByIdAsync( queuedTask.Id );
		}
	}
}
