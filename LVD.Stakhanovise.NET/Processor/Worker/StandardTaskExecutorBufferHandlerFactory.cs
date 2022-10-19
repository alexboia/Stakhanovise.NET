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

		public StandardTaskExecutorBufferHandlerFactory( ITaskExecutionMetricsProvider metricsProvider,
			IStakhanoviseLoggingProvider loggingProvider )
		{
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
			mLoggingProvider = loggingProvider
				?? throw new ArgumentNullException( nameof( loggingProvider ) );
		}

		public ITaskExecutorBufferHandler Create( ITaskBuffer taskBuffer, CancellationToken cancellationToken )
		{
			if ( taskBuffer == null )
				throw new ArgumentNullException( nameof( taskBuffer ) );

			IStakhanoviseLogger logger = CreateLogger();

			return new StandardTaskExecutorBufferHandler( taskBuffer,
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
