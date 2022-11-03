using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.RedisResultQueueBackup
{
	public class RedisTaskResultQueueBackup : ITaskResultQueueBackup
	{
		public Task PutAsync( IQueuedTaskResult result )
		{
			throw new NotImplementedException();
		}

		public Task RemoveAsync( IQueuedTaskResult result )
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<IQueuedTaskResult>> RetrieveBackedUpItemsAsync()
		{
			throw new NotImplementedException();
		}
	}
}
