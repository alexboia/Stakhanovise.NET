using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Statistics
{
	public class GenericCounts
	{
		public long TotalTasksInQueue { get; set; }

		public long TotalResultsInResultQueue { get; set; }

		public long TotalCompletedResultsInResultsQueue { get; set; }
	}
}
