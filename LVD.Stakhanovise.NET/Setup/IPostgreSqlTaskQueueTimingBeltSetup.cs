using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IPostgreSqlTaskQueueTimingBeltSetup
	{
		IPostgreSqlTaskQueueTimingBeltSetup WithTimeId ( Guid timeId );

		IPostgreSqlTaskQueueTimingBeltSetup SetupConnection ( Action<IConnectionSetup> setupAction );

		IPostgreSqlTaskQueueTimingBeltSetup WithInitialWallclockTimeCost (int initialWallclockTimeCost );

		IPostgreSqlTaskQueueTimingBeltSetup WithTimeTickBatchSize ( int timeTickBatchSize );

		IPostgreSqlTaskQueueTimingBeltSetup WithTimeTickMaxFailCount ( int timeTickMaxFailCount );
	}
}
