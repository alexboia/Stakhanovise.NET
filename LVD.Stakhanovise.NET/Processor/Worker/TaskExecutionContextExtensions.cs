using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Processor.Worker
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
