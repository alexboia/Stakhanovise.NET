using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class TestOptions
	{
		public static readonly QueuedTaskMapping DefaultMapping =
			new QueuedTaskMapping();

		public static PostgreSqlTaskQueueTimingBeltOptions GetDefaultPostgreSqlTaskQueueTimingBeltOptions ( Guid timeId, string connectionString )
		{
			return new PostgreSqlTaskQueueTimingBeltOptions( timeId,
				connectionOptions: new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				initialWallclockTimeCost: 1000,
				timeTickBatchSize: 10,
				timeTickMaxFailCount: 3 );
		}

		public static TaskProcessingOptions GetDefaultTaskProcessingOptions ()
		{
			return new TaskProcessingOptions( 1000,
				calculateDelayTicksTaskAfterFailure: token
					=> ( long )Math.Pow( 10, token.LastQueuedTaskResult.ErrorCount + 1 ),
				isTaskErrorRecoverable: ( task, exc )
					 => !( exc is NullReferenceException )
						 && !( exc is ArgumentException ),
				faultErrorThresholdCount: 5 );
		}

		public static TaskQueueConsumerOptions GetDefaultTaskQueueConsumerOptions ( string connectionString )
		{
			return new TaskQueueConsumerOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 5,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				mapping: DefaultMapping,
				queueConsumerConnectionPoolSize: 10 );
		}

		public static TaskQueueInfoOptions GetDefaultTaskQueueInfoOptions ( string connectionString )
		{
			return new TaskQueueInfoOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				mapping: DefaultMapping );
		}

		public static TaskQueueOptions GetDefaultTaskQueueProducerOptions ( string connectionString )
		{
			return new TaskQueueOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				DefaultMapping );
		}

		public static TaskQueueOptions GetDefaultTaskResultQueueOptions ( string connectionString )
		{
			return new TaskQueueOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 100 ),
				DefaultMapping );
		}
	}
}
