using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class InMemoryResultQueueBackup : ITaskResultQueueBackup
	{
		private readonly ConcurrentDictionary<Guid, IQueuedTaskResult> mStorage
			= new ConcurrentDictionary<Guid, IQueuedTaskResult>();

		public Task PutAsync( IQueuedTaskResult result )
		{
			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			mStorage [ result.Id ] = result;
			return Task.CompletedTask;
		}

		public Task RemoveAsync( IQueuedTaskResult result )
		{
			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			mStorage.TryRemove( result.Id, out IQueuedTaskResult removedResult );
			return Task.CompletedTask;
		}

		public Task<IEnumerable<IQueuedTaskResult>> RetrieveBackedUpItemsAsync()
		{
			return Task.FromResult<IEnumerable<IQueuedTaskResult>>( mStorage.Values );
		}
	}
}
