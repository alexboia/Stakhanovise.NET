using log4net;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultTaskResultQueue : ITaskResultQueue
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private ITaskQueueConsumer mTaskQueue;

		private bool mIsDisposed;

		public DefaultTaskResultQueue ( ITaskQueueConsumer taskQueue )
		{
			mTaskQueue = taskQueue ?? throw new ArgumentNullException( nameof( taskQueue ) );
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskResultQueue ), "Cannot reuse a disposed task result queue" );
		}

		public async Task EnqueueResultAsync ( QueuedTask queuedTask, TaskExecutionResult result )
		{
			CheckDisposedOrThrow();

			if ( queuedTask == null )
				throw new ArgumentNullException( nameof( queuedTask ) );

			try
			{
				if ( result != null )
				{
					//If the task did not execute successfully, notify the queue of the error;
					//  otherwise mark it as completed
					if ( !result.ExecutedSuccessfully )
						await mTaskQueue.NotifyTaskErroredAsync( queuedTask.Id,
							result );
					else
						await mTaskQueue.NotifyTaskCompletedAsync( queuedTask.Id,
							result );
				}

				//If there is no result, simply release the task - 
				//  we don't really have anything else to do
				else
					await mTaskQueue.ReleaseLockAsync( queuedTask.Id );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error finalizing task processing", exc );
			}
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				//We are not responsible for managing the lifecycle 
				//  of the task queue, so we will not be disposing it over here
				if ( disposing )
					mTaskQueue = null;

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
		}
	}
}
