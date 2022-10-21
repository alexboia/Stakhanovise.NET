using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Options;
using System;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueNotificationListenerFactory : ITaskQueueNotificationListenerFactory
	{
		private readonly ITaskQueueNotificationListenerMetricsProvider mMetricsProvider;

		private readonly IStakhanoviseLoggingProvider mLoggingProvider;

		public PostgreSqlTaskQueueNotificationListenerFactory( ITaskQueueNotificationListenerMetricsProvider metricsProvider,
			IStakhanoviseLoggingProvider loggingProvider )
		{
			mMetricsProvider = metricsProvider
				?? throw new ArgumentNullException( nameof( metricsProvider ) );
			mLoggingProvider = loggingProvider
				?? throw new ArgumentNullException( nameof( loggingProvider ) );
		}

		public ITaskQueueNotificationListener CreateListener( TaskQueueListenerOptions options )
		{
			return new PostgreSqlTaskQueueNotificationListener( options,
				mMetricsProvider,
				CreateLogger() );
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return mLoggingProvider.CreateLogger( typeof( PostgreSqlTaskQueueNotificationListener )
				.FullName );
		}
	}
}
