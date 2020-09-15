using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface IQueuedTaskToken : IDisposable
	{
		Task<bool> TrySetStartedAsync ( long estimatedProcessingTimeMillisencods );

		Task<bool> TrySetResultAsync ( TaskExecutionResult result, long actualProcessingTimeMilliseconds );

		Task ReleaseLockAsync ();

		IQueuedTask QueuedTask { get; }

		CancellationToken CancellationToken { get; }

		bool IsPending { get; }

		bool IsActive { get; }

		bool IsLocked { get; }
	}
}
