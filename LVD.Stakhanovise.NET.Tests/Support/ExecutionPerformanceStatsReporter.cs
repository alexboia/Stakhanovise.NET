using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ExecutionPerformanceStatsReporter
	{
		private IExecutionPerformanceMonitor mPerfMon;

		private ConcurrentQueue<Tuple<string, long>> mPerformanceStatsToReport;

		public ExecutionPerformanceStatsReporter( IExecutionPerformanceMonitor perfMon, IEnumerable<Tuple<string, long>> performanceStatsToReport )
		{
			if ( perfMon == null )
				throw new ArgumentNullException( nameof( perfMon ) );

			if ( performanceStatsToReport == null )
				throw new ArgumentNullException( nameof( performanceStatsToReport ) );

			mPerfMon = perfMon;
			mPerformanceStatsToReport = new ConcurrentQueue<Tuple<string, long>>( performanceStatsToReport );
		}

		public async Task ReportExecutionPerformancesStatsAsync()
		{

			while ( mPerformanceStatsToReport.TryDequeue( out Tuple<string, long> execTime ) )
				await mPerfMon.ReportExecutionTimeAsync( execTime.Item1, execTime.Item2, 0 );
		}
	}
}
