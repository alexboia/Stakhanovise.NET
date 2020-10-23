using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardPostgreSqlAppMetricsMonitorWriterSetup : IPostgreSqlAppMetricsMonitorWriterSetup
	{
		private QueuedTaskMapping mMapping;

		private StandardConnectionSetup mConnectionSetup;

		public StandardPostgreSqlAppMetricsMonitorWriterSetup ( StandardConnectionSetup connectionSetup,
			StakhanoviseSetupDefaults defaults)
		{
			mConnectionSetup = connectionSetup
				?? throw new ArgumentNullException( nameof( connectionSetup ) );

			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mMapping = defaults.Mapping;
		}

		public IPostgreSqlAppMetricsMonitorWriterSetup WithConnectionOptions ( Action<IConnectionSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mConnectionSetup );
			return this;
		}

		public IPostgreSqlAppMetricsMonitorWriterSetup WithMapping ( QueuedTaskMapping mapping )
		{
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );
			mMapping = mapping;
			return this;
		}

		public PostgreSqlAppMetricsMonitorWriterOptions BuildOptions()
		{
			return new PostgreSqlAppMetricsMonitorWriterOptions( mConnectionSetup.BuildOptions(), 
				mapping: mMapping );
		}
	}
}
