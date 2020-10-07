﻿using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskResultQueue
	{
		Task PostResultAsync ( IQueuedTaskToken token, int timeoutMilliseconds );

		Task PostResultAsync ( IQueuedTaskToken token );

		Task StartAsync ();

		Task StopAsync ();

		bool IsRunning { get; }
	}
}
