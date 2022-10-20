using LVD.Stakhanovise.NET.Queue;
using System;

namespace LVD.Stakhanovise.NET.Processor
{
	public static class TaskExecutionContextExtensions
	{
		public static IQueuedTaskToken ExtractQueuedTaskToken( this ITaskExecutionContext executionContext )
		{
			if ( executionContext == null )
				throw new ArgumentNullException( nameof( executionContext ) );

			return ( ( TaskExecutionContext ) executionContext )
				.DequeuedTaskToken;
		}
	}
}
