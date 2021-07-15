using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using System.Linq;
using System.Collections.Concurrent;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ExecutionPerformanceMonitorWriterMockWithDataCollection
	{
		private Mock<IExecutionPerformanceMonitorWriter> mWriterMock =
			new Mock<IExecutionPerformanceMonitorWriter>();

		private ConcurrentBag<TaskPerformanceStats> mActualWrittenStats =
			new ConcurrentBag<TaskPerformanceStats>();

		public ExecutionPerformanceMonitorWriterMockWithDataCollection( string processId )
		{
			mWriterMock
				.Setup( w => w.WriteAsync( processId, It.IsAny<IEnumerable<TaskPerformanceStats>>() ) )
				.Callback<string, IEnumerable<TaskPerformanceStats>>( RecordWrittenStats );
		}

		private void RecordWrittenStats( string processId, IEnumerable<TaskPerformanceStats> writerStats )
		{
			writerStats.ToList().ForEach( wsItem => mActualWrittenStats.Add( wsItem ) );
		}

		public void AssertStatsWrittenCorrectly(IEnumerable<TaskPerformanceStats> expectedWrittenStats )
		{
			CollectionAssert.AreEquivalent( expectedWrittenStats.ToArray(),
				mActualWrittenStats.ToArray() );
		}

		public IEnumerable<TaskPerformanceStats> ActualWrittenStats
			=> mActualWrittenStats;

		public IExecutionPerformanceMonitorWriter Object
			=> mWriterMock.Object;
	}
}
