using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class TestOptions
	{
		public static readonly QueuedTaskStatus[] ProcessWithStatuses = new QueuedTaskStatus[] {
			QueuedTaskStatus.Unprocessed,
			QueuedTaskStatus.Error,
			QueuedTaskStatus.Faulted,
			QueuedTaskStatus.Processing
		};


		public static TaskProcessingOptions GetDefaultTaskProcessingOptions ()
		{
			return new TaskProcessingOptions( 1000,
				defaultEstimatedProcessingTimeMilliseconds: 1000,
				calculateDelayTicksTaskAfterFailure: errorCount
					=> ( long )Math.Pow( 10, errorCount ),
				calculateEstimatedProcessingTimeMilliseconds: ( task, stats )
					=> stats.LongestExecutionTime > 0
						? stats.LongestExecutionTime
						: 1000,
				isTaskErrorRecoverable: ( task, exc )
					 => !( exc is NullReferenceException )
						 && !( exc is ArgumentException ) );
		}

		public static TaskQueueConsumerOptions GetDefaultTaskQueueConsumerOptions ( string connectionString )
		{
			return new TaskQueueConsumerOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 5,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),

				new QueuedTaskMapping(),
				ProcessWithStatuses,
				queueConsumerConnectionPoolSize: 10,
				faultErrorThresholdCount: 5 );
		}

		public static TaskQueueInfoOptions GetDefaultTaskQueueInfoOptions ( string connectionString )
		{
			return new TaskQueueInfoOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				new QueuedTaskMapping(),
				ProcessWithStatuses );
		}
	}
}
