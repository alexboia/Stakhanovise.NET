using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockQueuedTaskToken : IQueuedTaskToken
	{
		private QueuedTask mQueuedTask;

		private QueuedTaskResult mLastQueuedTaskResult;

		public MockQueuedTaskToken ( QueuedTask queuedTask, QueuedTaskResult lastQueuedTaskResult )
		{
			mQueuedTask = queuedTask;
			mLastQueuedTaskResult = lastQueuedTaskResult;
		}

		public MockQueuedTaskToken ( Guid queuedTaskId )
		{
			mQueuedTask = new QueuedTask( queuedTaskId );
			mLastQueuedTaskResult = new QueuedTaskResult( mQueuedTask );
		}

		public QueuedTaskInfo UdpateFromExecutionResult ( TaskExecutionResult result )
		{
			return mLastQueuedTaskResult.UdpateFromExecutionResult( result );
		}

		public IQueuedTask DequeuedTask => mQueuedTask;

		public IQueuedTaskResult LastQueuedTaskResult => mLastQueuedTaskResult;

		public DateTimeOffset DequeuedAt => DateTimeOffset.UtcNow;
	}
}
