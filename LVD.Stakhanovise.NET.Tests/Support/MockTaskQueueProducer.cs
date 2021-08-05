using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockTaskQueueProducer : ITaskQueueProducer
	{
		private const int ImmediatePastSecondsInterval = 1;

		private List<IQueuedTask> mProducedTasks = new List<IQueuedTask>();

		private ITimestampProvider mTimestampProvider;

		public MockTaskQueueProducer( ITimestampProvider timestampProvider )
		{
			mTimestampProvider = timestampProvider;
		}

		public async Task<IQueuedTask> EnqueueAsync<TPayload>( TPayload payload, string source, int priority )
		{
			return await EnqueueAsync( new QueuedTaskProduceInfo()
			{
				Payload = payload,
				Type = DeterminePayloadTypeFullName<TPayload>(),
				Priority = priority,
				Source = source,
				LockedUntilTs = GenerateImmediatePastLockedTimestamp(),
				Status = QueuedTaskStatus.Unprocessed
			} );
		}

		private string DeterminePayloadTypeFullName<TPayload>()
		{
			return typeof( TPayload )
				.FullName;
		}

		private DateTimeOffset GenerateImmediatePastLockedTimestamp()
		{
			return mTimestampProvider
				.GetNow()
				.AddSeconds( -ImmediatePastSecondsInterval );
		}

		public Task<IQueuedTask> EnqueueAsync( QueuedTaskProduceInfo queuedTaskInfo )
		{
			QueuedTask newTask = CreateNewTaskFromInfo( queuedTaskInfo );
			mProducedTasks.Add( newTask );
			return Task.FromResult<IQueuedTask>( newTask );
		}

		private QueuedTask CreateNewTaskFromInfo( QueuedTaskProduceInfo queuedTaskInfo )
		{
			return queuedTaskInfo.CreateNewTask( mTimestampProvider );
		}

		public ITimestampProvider TimestampProvider
			=> mTimestampProvider;

		public IEnumerable<IQueuedTask> ProducedTasks
			=> mProducedTasks;

		public int ProducedTasksCount
			=> mProducedTasks.Count;
	}
}
