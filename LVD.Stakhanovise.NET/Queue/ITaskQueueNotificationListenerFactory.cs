using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueNotificationListenerFactory
	{
		ITaskQueueNotificationListener CreateListener( TaskQueueListenerOptions options );
	}
}
