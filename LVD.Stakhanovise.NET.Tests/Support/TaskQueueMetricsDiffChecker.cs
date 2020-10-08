using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TaskQueueMetricsDiffChecker : IDisposable
	{
		private Func<Task<TaskQueueMetrics>> mAcquireMetricsFn;

		private TaskQueueMetrics mInitialMetrics;

		public TaskQueueMetricsDiffChecker ( Func<Task<TaskQueueMetrics>> acquireMetricsFn )
		{
			mAcquireMetricsFn = acquireMetricsFn;
		}

		public async Task CaptureInitialMetricsAsync ()
		{
			mInitialMetrics = await mAcquireMetricsFn.Invoke();
			Assert.NotNull( mInitialMetrics );
		}

		public async Task CaptureNewMetricsAndAssertCorrectDiff(TaskQueueMetrics delta)
		{
			TaskQueueMetrics newMetrics = await mAcquireMetricsFn
				.Invoke();
			
			Assert.NotNull( newMetrics );

			Assert.AreEqual( mInitialMetrics.TotalErrored + delta.TotalErrored,
				newMetrics.TotalErrored );
			Assert.AreEqual( mInitialMetrics.TotalFataled + delta.TotalFataled,
				newMetrics.TotalFataled );
			Assert.AreEqual( mInitialMetrics.TotalFaulted + delta.TotalFaulted,
				newMetrics.TotalFaulted );
			Assert.AreEqual( mInitialMetrics.TotalProcessed + delta.TotalProcessed,
				newMetrics.TotalProcessed );
			Assert.AreEqual( mInitialMetrics.TotalUnprocessed + delta.TotalUnprocessed,
				newMetrics.TotalUnprocessed );
		}

		public void Dispose()
		{
			mInitialMetrics = null;
			mAcquireMetricsFn = null;
		}
	}
}
