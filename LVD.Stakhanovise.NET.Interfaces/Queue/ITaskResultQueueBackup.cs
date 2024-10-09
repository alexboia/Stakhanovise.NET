using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public interface ITaskResultQueueBackup
	{
		Task InitAsync();
		
		Task PutAsync( IQueuedTaskResult result );

		Task RemoveAsync( IQueuedTaskResult result );

		Task<IEnumerable<IQueuedTaskResult>> RetrieveBackedUpItemsAsync();
	}
}
