using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueNotificationListenerDiagnostics
	{
		public PostgreSqlTaskQueueNotificationListenerDiagnostics ( int notificationCount,
			int reconnectCount,
			int processId )
		{
			NotificationCount = notificationCount;
			ReconnectCount = reconnectCount;
			ConnectionBackendProcessId = processId;
		}

		public static PostgreSqlTaskQueueNotificationListenerDiagnostics Zero
			=> new PostgreSqlTaskQueueNotificationListenerDiagnostics( 0, 0, 0 );

		public int NotificationCount { get; private set; }

		public int ReconnectCount { get; private set; }

		public int WaitTimeoutCount { get; private set; }

		public int ConnectionBackendProcessId { get; private set; }
	}
}
