using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueNotificationListener
	{
		event EventHandler<NewTaskPostedEventArgs> NewTaskPosted;

		event EventHandler<ListenerConnectedEventArgs> ListenerConnected;

		event EventHandler<ListenerConnectionRestoredEventArgs> ListenerConnectionRestored;

		event EventHandler<ListenerTimedOutEventArgs> ListenerTimedOutWhileWaiting;

		Task StartAsync ();

		Task StopAsync ();

		bool IsStarted { get; }
	}
}
