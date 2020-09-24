using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class TaskQueueConsumerOptions : TaskQueueOptions
	{
		public TaskQueueConsumerOptions ( ConnectionOptions generalConnectionOptions, int queueConsumerConnectionPoolSize )
			: base( generalConnectionOptions )
		{
			if ( queueConsumerConnectionPoolSize < 1 )
				throw new ArgumentOutOfRangeException( nameof( queueConsumerConnectionPoolSize ),
					"Queue consumer connection pool size must be greater than or equal to 1" );

			QueueConsumerConnectionPoolSize = queueConsumerConnectionPoolSize;
		}

		public int QueueConsumerConnectionPoolSize { get; private set; }

		public int FaultErrorThresholdCount { get; private set; }
	}
}
