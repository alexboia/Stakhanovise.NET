using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueProducer
	{
		Task<QueuedTask> EnqueueAsync<TPayload> ( TPayload payload,
			string source,
			int priority );
	}
}
