using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskResultQueue
	{
		Task PostResultAsync ( IQueuedTaskResult result, int timeoutMilliseconds );

		Task PostResultAsync ( IQueuedTaskResult result );

		Task StartAsync ();

		Task StopAsync ();

		bool IsRunning { get; }
	}
}
