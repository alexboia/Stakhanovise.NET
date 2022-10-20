using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LVD.Stakhanovise.NET.Logging;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutorBufferHandlerFactory : ITaskExecutorBufferHandlerFactory
	{
		private readonly ITaskExecutionMetricsProvider mMetricsProvider;

		private readonly IStakhanoviseLoggingProvider mLoggingProvider;

		private readonly ITaskBuffer mTaskBuffer;

		public StandardTaskExecutorBufferHandlerFactory( ITaskBuffer taskBuffer,
			ITaskExecutionMetricsProvider metricsProvider,
			IStakhanoviseLoggingProvider loggingProvider )
		{
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
			mLoggingProvider = loggingProvider
				?? throw new ArgumentNullException( nameof( loggingProvider ) );
		}

		public ITaskExecutorBufferHandler Create( CancellationToken cancellationToken )
		{
			IStakhanoviseLogger logger = 
				CreateLogger();

			return new StandardTaskExecutorBufferHandler( mTaskBuffer,
				mMetricsProvider,
				cancellationToken,
				logger );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return mLoggingProvider.CreateLogger( typeof( StandardTaskExecutorBufferHandler )
				.FullName );
		}
	}
}
