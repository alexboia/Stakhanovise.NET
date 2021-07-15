// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StandardExecutionPerformanceMonitorTests
	{
		private string mTestProcessId;

		public StandardExecutionPerformanceMonitorTests()
		{
			mTestProcessId = Guid.NewGuid()
				.ToString();
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public async Task Test_CanReportExecutionStats_SerialCalls( int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor( mTestProcessId );

			IEnumerable<Tuple<string, long>> perfStatsToReport = GenerateSamplePerformanceStats( nReports,
				out List<TaskPerformanceStats> expectedWrittenStats );

			ExecutionPerformanceStatsReporter perfStatsReporter =
				new ExecutionPerformanceStatsReporter( perfMon, perfStatsToReport );

			ExecutionPerformanceMonitorWriterMockWithDataCollection writerMock =
				new ExecutionPerformanceMonitorWriterMockWithDataCollection( mTestProcessId );

			await perfMon.StartFlushingAsync( writerMock.Object );
			await perfStatsReporter.ReportExecutionPerformancesStatsAsync();
			await perfMon.StopFlushingAsync();

			writerMock.AssertStatsWrittenCorrectly( expectedWrittenStats );
		}

		[TestCase( 1, 2 )]
		[TestCase( 1, 5 )]
		[TestCase( 1, 10 )]
		[TestCase( 5, 2 )]
		[TestCase( 5, 5 )]
		[TestCase( 5, 10 )]
		[TestCase( 100, 2 )]
		[TestCase( 100, 5 )]
		[TestCase( 100, 10 )]
		[Repeat( 10 )]
		public async Task Test_CanReportExecutionStats_ParallelCalls( int nReports, int nWorkers )
		{

			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor( mTestProcessId );

			IEnumerable<Tuple<string, long>> perfStatsToReport = GenerateSamplePerformanceStats( nReports,
				out List<TaskPerformanceStats> expectedWrittenStats );

			ExecutionPerformanceMonitorWriterMockWithDataCollection writerMock =
				new ExecutionPerformanceMonitorWriterMockWithDataCollection( mTestProcessId );

			ExecutionPerformanceStatsReporter perfStatsReporter =
				new ExecutionPerformanceStatsReporter( perfMon, perfStatsToReport );

			await perfMon.StartFlushingAsync( writerMock.Object );
			await ConcurrentlyReportStatsAsync( perfStatsReporter, nWorkers );
			await perfMon.StopFlushingAsync();

			writerMock.AssertStatsWrittenCorrectly( expectedWrittenStats );
		}

		private async Task ConcurrentlyReportStatsAsync( ExecutionPerformanceStatsReporter perfStatsReporter, int nWorkers )
		{
			Task[] workers = CreateAndStartPerfStatsConcurrentWorkers( perfStatsReporter, nWorkers );
			await Task.WhenAll( workers );
		}

		private Task[] CreateAndStartPerfStatsConcurrentWorkers( ExecutionPerformanceStatsReporter perfStatsReporter, int nWorkers )
		{
			Task[] workers = new Task[ nWorkers ];
			for ( int i = 0; i < nWorkers; i++ )
				workers[ i ] = Task.Run( async () => await perfStatsReporter.ReportExecutionPerformancesStatsAsync() );
			return workers;
		}

		private IEnumerable<Tuple<string, long>> GenerateSamplePerformanceStats( int count,
			out List<TaskPerformanceStats> expectedWrittenStats )
		{
			Faker faker =
				new Faker();

			ConcurrentQueue<Tuple<string, long>> execTimes =
				new ConcurrentQueue<Tuple<string, long>>();

			expectedWrittenStats = faker.RandomExecutionPerformanceStats( count );
			foreach ( TaskPerformanceStats s in expectedWrittenStats )
			{
				execTimes.Enqueue( new Tuple<string, long>(
					s.PayloadType,
					s.DurationMilliseconds ) );
			}

			return execTimes;
		}
	}
}
