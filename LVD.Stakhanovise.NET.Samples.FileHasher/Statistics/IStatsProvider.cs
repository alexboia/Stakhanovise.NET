using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Statistics
{
	public interface IStatsProvider
	{
		Task<GenericCounts> ComputeGenericCountsAsync();
	}
}
