using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlQueuedTaskTokenConnectionStateChangeArgs : System.EventArgs
	{
		public PostgreSqlQueuedTaskTokenConnectionStateChangeArgs( PostgreSqlQueuedTaskTokenConnectionState newState)
		{
			NewState = newState;
		}

		public PostgreSqlQueuedTaskTokenConnectionState NewState { get; private set; }
	}
}
