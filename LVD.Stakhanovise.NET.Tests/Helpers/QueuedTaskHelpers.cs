using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class QueuedTaskHelpers
	{
		public static QueuedTaskToken CreateQueuedTaskToken( int errorCount = 0 )
		{
			SampleTaskPayload payload = new SampleTaskPayload();
			return CreateQueuedTaskToken( payload, errorCount );
		}

		public static QueuedTaskToken CreateQueuedTaskToken( object payload, int errorCount = 0 )
		{
			QueuedTask task = CreateQueuedTask( payload );
			QueuedTaskResult taskResult = new QueuedTaskResult( task );

			taskResult.ErrorCount = errorCount;
			if ( errorCount > 0 )
				taskResult.LastError = new QueuedTaskError( "Some error" );

			return new QueuedTaskToken( task,
				taskResult,
				DateTimeOffset.UtcNow );
		}

		public static QueuedTask CreateQueuedTask()
		{
			SampleTaskPayload payload = new SampleTaskPayload();
			return CreateQueuedTask( payload );
		}

		public static QueuedTask CreateQueuedTask( object payload )
		{
			return new QueuedTask()
			{
				Id = Guid.NewGuid(),
				Payload = payload,
				Source = Guid.NewGuid()
					.ToString(),
				Type = payload.GetType()
					.FullName,
			};
		}
	}
}
