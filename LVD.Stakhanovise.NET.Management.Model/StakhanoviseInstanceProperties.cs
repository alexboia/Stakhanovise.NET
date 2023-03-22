using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Management.Model
{
	public class StakhanoviseInstanceProperties
	{
		public string QueueTableName
		{
			get; set;
		}

		public string ResultsQueueTableName
		{
			get; set;
		}

		public string NewTaskNotificationChannelName
		{
			get; set;
		}

		public string ExecutionTimeStatsTableName
		{
			get; set;
		}

		public string MetricsTableName
		{
			get; set;
		}

		public string DequeueFunctionName
		{
			get; set;
		}
	}
}
