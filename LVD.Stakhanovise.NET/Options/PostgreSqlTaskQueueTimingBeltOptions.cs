using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class PostgreSqlTaskQueueTimingBeltOptions
	{
		public PostgreSqlTaskQueueTimingBeltOptions ( Guid timeId, ConnectionOptions connectionOptions )
		{
			TimeId = timeId;
			ConnectionOptions = connectionOptions
				?? throw new ArgumentNullException( nameof( connectionOptions ) );

			InitialWallclockTimeCost = 1000;
			TimeTickBatchSize = 5;
			TimeTickMaxFailCount = 3;
		}

		public Guid TimeId { get; private set; }

		public ConnectionOptions ConnectionOptions { get; private set; }

		public int InitialWallclockTimeCost { get; private set; }

		public int TimeTickBatchSize { get; private set; }

		public int TimeTickMaxFailCount { get; private set; }
	}
}
