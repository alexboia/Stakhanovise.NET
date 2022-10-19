using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using System;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskExecutorResolver : ITaskExecutorResolver
	{
		private readonly ITaskExecutorRegistry mExecutorRegistry;

		private readonly IStakhanoviseLogger mLogger;

		public StandardTaskExecutorResolver( ITaskExecutorRegistry executorRegistry,
			IStakhanoviseLogger logger )
		{
			mExecutorRegistry = executorRegistry
				?? throw new ArgumentNullException( nameof( executorRegistry ) );
			mLogger = logger
				?? throw new ArgumentNullException( nameof( logger ) );
		}

		public ITaskExecutor ResolveExecutor( IQueuedTask queuedTask )
		{
			if ( queuedTask == null )
				throw new ArgumentNullException( nameof( queuedTask ) );

			ITaskExecutor taskExecutor = null;
			Type payloadType = DetectTaskPayloadType( queuedTask );

			if ( payloadType != null )
			{
				mLogger.DebugFormat( "Runtime payload type {0} found for task type {1}.",
					payloadType,
					queuedTask.Type );

				taskExecutor = ResolveExecutor( payloadType );
				if ( taskExecutor != null )
					mLogger.DebugFormat( "Executor {0} found for task type {1}.",
						taskExecutor.GetType().FullName,
						queuedTask.Type );
				else
					mLogger.WarnFormat( "Executor not found for task type {0}.",
						queuedTask.Type );

			}
			else
				mLogger.WarnFormat( "Runtime payload type not found for task type {0}.",
					queuedTask.Type );

			return taskExecutor;
		}

		private Type DetectTaskPayloadType( IQueuedTask queuedTask )
		{
			return queuedTask.Payload != null
				? queuedTask.Payload.GetType()
				: mExecutorRegistry.ResolvePayloadType( queuedTask.Type );
		}

		private ITaskExecutor ResolveExecutor( Type payloadType )
		{
			return mExecutorRegistry
				.ResolveExecutor( payloadType );
		}
	}
}
