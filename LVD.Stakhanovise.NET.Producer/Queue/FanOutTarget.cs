using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class FanOutTarget
	{
		public FanOutTarget( IEnumerable<ITaskQueueProducer> producers, Func<QueuedTaskProduceInfo, bool> predicate, int priority )
		{
			Producers = producers
				?? throw new ArgumentNullException( nameof( producers ) );
			Predicate = predicate
				?? throw new ArgumentNullException( nameof( predicate ) );
			Priority = priority;
		}

		public IEnumerable<ITaskQueueProducer> Producers
		{
			get;
		}

		public Func<QueuedTaskProduceInfo, bool> Predicate
		{
			get;
		}

		public int Priority
		{
			get;
		}
	}
}
