using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public enum ClearForDequeReason
	{
		NewTaskPostedNotificationReceived = 0x01,
		NewTaskListenerConnectionStateChange = 0x02
	}
}
