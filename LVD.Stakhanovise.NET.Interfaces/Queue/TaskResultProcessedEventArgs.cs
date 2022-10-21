using LVD.Stakhanovise.NET.Model;
using System;

namespace LVD.Stakhanovise.NET.Queue
{
	public class TaskResultProcessedEventArgs : EventArgs
	{
		public TaskResultProcessedEventArgs( IQueuedTaskResult result )
		{
			Result = result;
		}

		public IQueuedTaskResult Result
		{
			get; private set;
		}
	}
}
