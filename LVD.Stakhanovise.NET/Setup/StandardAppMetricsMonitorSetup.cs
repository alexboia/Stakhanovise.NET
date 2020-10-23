using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardAppMetricsMonitorSetup : IAppMetricsMonitorSetup
	{
		private int mCollectionIntervalMilliseconds;

		public StandardAppMetricsMonitorSetup ( StakhanoviseSetupDefaults defaults )
		{
			mCollectionIntervalMilliseconds = defaults.AppMetricsCollectionIntervalMilliseconds;
		}

		public IAppMetricsMonitorSetup WithCollectionIntervalMilliseconds ( int collectionIntervalMilliseconds )
		{
			if ( collectionIntervalMilliseconds < 1 )
				throw new ArgumentOutOfRangeException( nameof( collectionIntervalMilliseconds ),
					"The collection interval must be greater than 0" );

			mCollectionIntervalMilliseconds = collectionIntervalMilliseconds;
			return this;
		}

		public IAppMetricsMonitor BuildMonitor ( IAppMetricsMonitorWriter writer )
		{
			return new StandardAppMetricsMonitor( new AppMetricsMonitorOptions( mCollectionIntervalMilliseconds ),
				writer: writer );
		}
	}
}
