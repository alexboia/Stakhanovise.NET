using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskQueueAbstractTimeProvider
	{
		Task<long> ComputeAbsoluteTimeTicksAsync ( long timeTicksToAdd );

		Task<AbstractTimestamp> GetCurrentTimeAsync ();
	}
}
