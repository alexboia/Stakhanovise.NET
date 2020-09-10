using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueConsumer
	{
		event EventHandler<ClearForDequeueEventArgs> ClearForDequeue;

		Task StartReceivingNewTaskUpdatesAsync ();

		Task StopReceivingNewTaskUpdatesAsync ();

		Task<QueuedTask> NotifyTaskCompletedAsync ( Guid queuedTaskId, TaskExecutionResult result );

		Task<QueuedTask> NotifyTaskErroredAsync ( Guid queuedTaskId, TaskExecutionResult result );

		Task<QueuedTask> DequeueAsync ( params string[] supportedTypes );

		Task ReleaseLockAsync ( Guid queuedTaskId );

		bool IsReceivingNewTaskUpdates { get; }

		int FaultErrorThresholdCount { get; set; }

		int DequeuePoolSize { get; }
	}
}
