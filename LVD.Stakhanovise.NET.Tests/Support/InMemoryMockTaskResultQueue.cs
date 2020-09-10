using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class InMemoryMockTaskResultQueue : ITaskResultQueue
	{
		private ConcurrentDictionary<QueuedTask, TaskExecutionResult> mTaskResults
			= new ConcurrentDictionary<QueuedTask, TaskExecutionResult>();

		public Task EnqueueResultAsync ( QueuedTask queuedTask, TaskExecutionResult executionResult )
		{
			mTaskResults.TryAdd( queuedTask, executionResult );
			return Task.CompletedTask;
		}

		public void Dispose ()
		{
			mTaskResults.Clear();
		}

		public IDictionary<QueuedTask, TaskExecutionResult> TaskResults
			=> mTaskResults;
	}
}
