using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlQueuedTaskTokenConnectionStats
	{
		public PostgreSqlQueuedTaskTokenConnectionStats ( TimeSpan connectionEstablishmentDuration )
			: this( ( long )Math.Ceiling( connectionEstablishmentDuration.TotalMilliseconds ) )
		{
			return;
		}

		public PostgreSqlQueuedTaskTokenConnectionStats ( long connectionEstablishmentDuration )
		{
			ConnectCount = 1;
			AvgConnectionEstDuration = connectionEstablishmentDuration;
		}

		public void IncrementConnectCount ( TimeSpan lastEstablishmentDuration )
		{
			IncrementConnectCount( ( long )Math.Ceiling( lastEstablishmentDuration.TotalMilliseconds ) );
		}

		public void IncrementConnectCount ( long lastEstablishmentDurationMilliseconds )
		{
			AvgConnectionEstDuration = ( ConnectCount * AvgConnectionEstDuration + lastEstablishmentDurationMilliseconds )
				/ ( ConnectCount + 1 );
			ConnectCount++;
		}

		public long ConnectCount { get; private set; }

		public long AvgConnectionEstDuration { get; private set; }
	}
}
