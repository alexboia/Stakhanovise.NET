using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueStats
	{
		Task<TaskQueueMetrics> ComputeMetricsAsync ();

		Task<QueuedTask> PeekAsync ();
	}
}
