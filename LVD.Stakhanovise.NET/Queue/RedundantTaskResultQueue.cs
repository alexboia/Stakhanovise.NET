using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class RedundantTaskResultQueue : ITaskResultQueue
	{
		public event EventHandler<TaskResultProcessedEventArgs> TaskResultProcessed;

		private readonly ITaskResultQueue mMainResultQueue;

		private readonly ITaskResultQueueBackup mResultQueueBackup;

		private readonly IStakhanoviseLogger mLogger;

		public RedundantTaskResultQueue( ITaskResultQueue mainResultQueue,
			ITaskResultQueueBackup resultQueueBackup,
			IStakhanoviseLogger logger )
		{
			mMainResultQueue = mainResultQueue
				?? throw new ArgumentNullException( nameof( mainResultQueue ) );
			mResultQueueBackup = resultQueueBackup
				?? throw new ArgumentNullException( nameof( resultQueueBackup ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		public async Task PostResultAsync( IQueuedTaskResult result )
		{
			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			await StoreToBackupAsync( result );
			await PostToMainQueueAsync( result );
		}

		private async Task StoreToBackupAsync( IQueuedTaskResult result )
		{
			try
			{
				await mResultQueueBackup.PutAsync( result );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Failed to store token result to backup", exc );
			}
		}

		private async Task PostToMainQueueAsync( IQueuedTaskResult result )
		{
			await mMainResultQueue.PostResultAsync( result );
		}

		public async Task StartAsync()
		{
			try
			{
				if ( !mMainResultQueue.IsRunning )
				{
					mMainResultQueue.TaskResultProcessed += OnTaskResultProcessed;
					await mMainResultQueue.StartAsync();
					await RestoreBackedupItemsAsync();
				}
			}
			catch ( Exception )
			{
				await StopAsync();
				throw;
			}
		}

		private void OnTaskResultProcessed( object sender, TaskResultProcessedEventArgs e )
		{
			RemoveFromBackup( e.Result );

			EventHandler<TaskResultProcessedEventArgs> handler = TaskResultProcessed;
			if ( handler != null )
				handler( this, new TaskResultProcessedEventArgs( e.Result ) );
		}

		private void RemoveFromBackup( IQueuedTaskResult result )
		{
			RemoveFromBackupAsync( result )
				.Wait();
		}

		private async Task RemoveFromBackupAsync( IQueuedTaskResult result )
		{
			try
			{
				await mResultQueueBackup.RemoveAsync( result );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error removing token result from back-up", exc );
			}
		}

		private async Task RestoreBackedupItemsAsync()
		{
			IEnumerable<IQueuedTaskResult> backedupItems =
				await RetrieveBackedUpItemsAsync();

			foreach ( IQueuedTaskResult backedupItem in backedupItems )
				await PostToMainQueueAsync( backedupItem );
		}

		private async Task<IEnumerable<IQueuedTaskResult>> RetrieveBackedUpItemsAsync()
		{
			return await mResultQueueBackup
				.RetrieveBackedUpItemsAsync();
		}

		public async Task StopAsync()
		{
			if ( mMainResultQueue.IsRunning )
			{
				mMainResultQueue.TaskResultProcessed -= OnTaskResultProcessed;
				await mMainResultQueue.StopAsync();
			}
		}

		public bool IsRunning =>
			mMainResultQueue.IsRunning;
	}
}
