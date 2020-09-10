using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class TaskQueueMetrics
	{
		public TaskQueueMetrics ( long totalUnprocessed,
			long totalErrored,
			long totalFaulted,
			long totalFataled,
			long totalProcessed )
		{
			TotalUnprocessed = totalUnprocessed;
			TotalErrored = totalErrored;
			TotalFaulted = totalFaulted;
			TotalFataled = totalFataled;
			TotalProcessed = totalProcessed;
		}

		public long TotalUnprocessed { get; private set; }

		public long TotalErrored { get; private set; }

		public long TotalFaulted { get; private set; }

		public long TotalFataled { get; private set; }

		public long TotalProcessed { get; private set; }
	}
}
