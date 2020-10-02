using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public enum PostgreSqlQueuedTaskTokenConnectionState
	{
		Dropped = 0x01,
		Established = 0x02,
		FailedPermanently = 0x03
	}
}
