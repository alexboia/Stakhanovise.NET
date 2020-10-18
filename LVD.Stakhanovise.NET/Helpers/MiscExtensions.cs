using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class MiscExtensions
	{
		public static void ReportExecutionTime ( this IExecutionPerformanceMonitor executionPerformanceMonitor,
			IQueuedTaskToken queuedTaskToken,
			TaskExecutionResult result )
		{
			if ( executionPerformanceMonitor == null )
				throw new ArgumentNullException( nameof( executionPerformanceMonitor ) );

			if ( queuedTaskToken == null )
				throw new ArgumentNullException( nameof( queuedTaskToken ) );

			if ( result == null )
				throw new ArgumentNullException( nameof( result ) );

			executionPerformanceMonitor.ReportExecutionTime(
				queuedTaskToken.DequeuedTask.Type,
				result.ProcessingTimeMilliseconds );
		}
	}
}
