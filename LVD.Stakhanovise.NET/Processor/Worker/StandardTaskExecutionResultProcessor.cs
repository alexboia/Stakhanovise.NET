using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutionResultProcessor : ITaskResultProcessor
	{
		private readonly ITaskResultQueue mTaskResultQueue;

		private readonly ITaskQueueProducer mTaskQueueProducer;

		private readonly IExecutionPerformanceMonitor mPerformanceMonitor;

		private readonly IStakhanoviseLogger mLogger;

		public StandardTaskExecutionResultProcessor( ITaskResultQueue taskResultQueue,
			ITaskQueueProducer taskQueueProducer,
			IExecutionPerformanceMonitor performanceMonitor,
			IStakhanoviseLogger logger )
		{
			mPerformanceMonitor = performanceMonitor
				?? throw new ArgumentNullException( nameof( performanceMonitor ) );
			mTaskResultQueue = taskResultQueue
				?? throw new ArgumentNullException( nameof( taskResultQueue ) );
			mTaskQueueProducer = taskQueueProducer
				?? throw new ArgumentNullException( nameof( taskQueueProducer ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		public async Task ProcessResultAsync( IQueuedTaskToken queuedTaskToken,
			TaskExecutionResult result )
		{
			if ( queuedTaskToken == null )
				throw new ArgumentNullException( nameof( queuedTaskToken ) );

			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			try
			{
				await DoProcessResultAsync( queuedTaskToken,
					result );
				await ReportExecutionTimeAsync( queuedTaskToken,
					result );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Failed to set queued task result. Task will be discarded.",
					exc );
			}
		}

		private async Task DoProcessResultAsync( IQueuedTaskToken queuedTaskToken,
			TaskExecutionResult result )
		{
			if ( !NeedsProcessing( result ) )
				return;

			//Update execution result and see whether 
			//	we need to repost the task to retry its execution
			QueuedTaskProduceInfo repostWithInfo = queuedTaskToken
				.UdpateFromExecutionResult( result );

			await PostResultAsync( queuedTaskToken );
			await RepostIfNeededAsync( repostWithInfo );
		}

		private bool NeedsProcessing( TaskExecutionResult result )
		{
			//There is no result - most likely, no executor found;
			//	nothing to process, just stop and return
			if ( !result.HasResult )
			{
				mLogger.Debug( "No result info returned. Task will be discarded." );
				return false;
			}

			//Execution has been cancelled, usually as a response 
			//	to a cancellation request;
			//	nothing to process, just stop and return
			if ( result.ExecutionCancelled )
			{
				mLogger.Debug( "Task execution cancelled. Task will be discarded." );
				return false;
			}

			return true;
		}

		private async Task PostResultAsync( IQueuedTaskToken queuedTaskToken )
		{
			mLogger.Debug( "Will post task execution result." );
			await mTaskResultQueue.PostResultAsync( queuedTaskToken );
			mLogger.Debug( "Successfully posted task execution result." );
		}

		private async Task RepostIfNeededAsync( QueuedTaskProduceInfo repostWithInfo )
		{
			//If the task needs to be reposted, do so
			if ( repostWithInfo != null )
			{
				mLogger.Debug( "Will repost task for execution." );
				await mTaskQueueProducer.EnqueueAsync( repostWithInfo );
				mLogger.Debug( "Sucessfully reposted task for execution." );
			}
			else
				mLogger.Debug( "Will not repost task for execution." );
		}

		private async Task ReportExecutionTimeAsync( IQueuedTaskToken queuedTaskToken,
			TaskExecutionResult result )
		{
			await mPerformanceMonitor.ReportExecutionTimeAsync( queuedTaskToken,
				result );
		}
	}
}
