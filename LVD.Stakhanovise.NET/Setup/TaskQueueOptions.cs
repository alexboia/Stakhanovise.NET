using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class TaskQueueOptions
	{
		public int LockPoolSize { get; private set; }

		public int FaultErrorThresholdCount { get; private set; }

		public Func<int, long> LockTaskAfterFailureCalculator { get; private set; }

		public QueuedTaskMapping Mapping { get; private set; }
	}
}
