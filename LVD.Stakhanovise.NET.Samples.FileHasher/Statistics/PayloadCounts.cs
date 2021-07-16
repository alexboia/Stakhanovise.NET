using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Statistics
{
	public class PayloadCounts
	{
		public Dictionary<string, long> TotalTasksInQueuePerPayload { get; set; }

		public Dictionary<string, long> TotalResultsInResultQueuePerPayload { get; set; }

		public Dictionary<string, long> TotalCompletedResultsInResultsQueuePerPayload { get; set; }
	}
}
