using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class TaskQueueOptions
	{
		public int WorkerCount { get; private set; }

		public int ConnectionKeepAlive { get; private set; }

		public string ConnectionString { get; private set; }

		public int FaultErrorThresholdCount { get; private set; }

		public Func<int, long> LockTaskAfterFailureCalculator { get; private set; }

		public int ConnectionRetryCount { get; private set; }

		public int ConnectionRetryDelay { get; private set; }

		public QueuedTaskMapping Mapping { get; private set; }

		public int DequeuePoolSize { get; private set; }
	}
}
