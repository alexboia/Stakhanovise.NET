using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutionRetryCalculator : ITaskExecutionRetryCalculator
	{
		private readonly TaskProcessingOptions mOptions;

		private readonly IStakhanoviseLogger mLogger;

		public StandardTaskExecutionRetryCalculator( TaskProcessingOptions options,
			IStakhanoviseLogger logger )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		public DateTimeOffset ComputeRetryAt( IQueuedTaskToken queuedTaskToken )
		{
			if ( queuedTaskToken == null )
				throw new ArgumentNullException( nameof( queuedTaskToken ) );

			long delayMilliseconds =
				CalculateRetryMillisecondsDelay( queuedTaskToken );

			return DateTimeOffset.UtcNow
				.AddMilliseconds( delayMilliseconds );
		}

		private long CalculateRetryMillisecondsDelay( IQueuedTaskToken queuedTaskToken )
		{
			try
			{
				return mOptions.CalculateRetryMillisecondsDelay( queuedTaskToken );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Failed to compute delay. Using default value of 0.",
					exc );
			}

			return 0;
		}
	}
}
