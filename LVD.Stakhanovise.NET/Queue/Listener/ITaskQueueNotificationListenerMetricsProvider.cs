using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueNotificationListenerMetricsProvider : IAppMetricsProvider
	{
		void IncrementTaskNotificationCount();

		void IncrementNotificationWaitTimeoutCount();

		void IncrementReconnectCount();
	}
}
