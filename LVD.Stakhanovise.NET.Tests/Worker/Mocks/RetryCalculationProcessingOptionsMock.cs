using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Tests.Worker.Mocks
{
	public class RetryCalculationProcessingOptionsMock
	{
		private readonly long mRetryMilliseconds;

		private readonly List<IQueuedTaskToken> mRetryCalculationCalledFor =
			new List<IQueuedTaskToken>();

		public RetryCalculationProcessingOptionsMock( long retryMilliseconds )
		{
			mRetryMilliseconds = retryMilliseconds;
		}

		private long DoCalculateRetryMillisecondsDelay( IQueuedTaskToken token )
		{
			mRetryCalculationCalledFor.Add( token );
			return mRetryMilliseconds;
		}

		public TaskProcessingOptions GetOptions()
		{
			return new TaskProcessingOptions( DoCalculateRetryMillisecondsDelay,
				isTaskErrorRecoverable: ( q, e ) => true,
				faultErrorThresholdCount: 1 );
		}

		public bool WasRetryCalculationCalledFor( IQueuedTaskToken token )
		{
			return mRetryCalculationCalledFor.Contains( token );
		}

		public int RetryCalculationCallCount
			=> mRetryCalculationCalledFor.Count;
	}
}
