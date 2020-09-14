using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueTimingBelt
	{
		void AddWallclockTimeCost ( long milliseconds );

		void AddWallclockTimeCost ( TimeSpan duration );

		Task<AbstractTimestamp> TickAbstractTimeAsync ( int timeout );

		Task StartAsync ();

		Task StopAsync ();

		AbstractTimestamp LastTime { get; }

		bool IsRunning { get; }
	}
}
