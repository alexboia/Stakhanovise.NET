using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardPostgreSqlTaskQueueTimingBeltSetup : IPostgreSqlTaskQueueTimingBeltSetup
	{
		private Guid mTimeId;

		private int mInitialWallclockTimeCost = 1000;

		private int mTimeTickBatchSize = 5;

		private int mTimeTickMaxFailCount = 3;

		private StandardConnectionSetup mConnectionSetup =
			new StandardConnectionSetup();

		public IPostgreSqlTaskQueueTimingBeltSetup SetupConnection ( Action<IConnectionSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mConnectionSetup );
			return this;
		}

		public IPostgreSqlTaskQueueTimingBeltSetup WithInitialWallclockTimeCost ( int initialWallclockTimeCost )
		{
			if ( initialWallclockTimeCost < 0 )
				throw new ArgumentOutOfRangeException( nameof( initialWallclockTimeCost ),
					"The initial wallclock time cost must be greater than or equal to zero" );

			mInitialWallclockTimeCost = initialWallclockTimeCost;
			return this;
		}

		public IPostgreSqlTaskQueueTimingBeltSetup WithTimeId ( Guid timeId )
		{
			mTimeId = timeId;
			return this;
		}

		public IPostgreSqlTaskQueueTimingBeltSetup WithTimeTickBatchSize ( int timeTickBatchSize )
		{
			if ( timeTickBatchSize <= 0 )
				throw new ArgumentOutOfRangeException( nameof( timeTickBatchSize ),
					"The initial time tick batch size must be greater than zero" );

			mTimeTickBatchSize = timeTickBatchSize;
			return this;
		}

		public IPostgreSqlTaskQueueTimingBeltSetup WithTimeTickMaxFailCount ( int timeTickMaxFailCount )
		{
			if ( timeTickMaxFailCount <= 0 )
				throw new ArgumentOutOfRangeException( nameof( timeTickMaxFailCount ),
					"The initial time tick max fail count must be greater than zero" );

			mTimeTickMaxFailCount = timeTickMaxFailCount;
			return this;
		}

		public PostgreSqlTaskQueueTimingBeltOptions BuildOptions ()
		{
			ConnectionOptions connectionOptions = mConnectionSetup
				.BuildOptions();

			return new PostgreSqlTaskQueueTimingBeltOptions( mTimeId, connectionOptions,
				initialWallclockTimeCost: mInitialWallclockTimeCost,
				timeTickBatchSize: mTimeTickBatchSize,
				timeTickMaxFailCount: mTimeTickMaxFailCount );
		}
	}
}
