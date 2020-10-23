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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Tests.Payloads;
using NUnit.Framework;
using Bogus;
using System.Linq;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Model;
using System.Threading;
using LVD.Stakhanovise.NET.Options;
using Moq;
using System.Diagnostics;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StandardExecutionPerformanceMonitorTests
	{
		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public async Task Test_CanReportExecutionStats_SerialCalls ( int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> perfStatsToReport = GenerateSamplePerformanceStats( nReports,
				out List<TaskPerformanceStats> expectedWrittenStats );

			Mock<IExecutionPerformanceMonitorWriter> writerMock =
				new Mock<IExecutionPerformanceMonitorWriter>();

			List<TaskPerformanceStats> actualWrittenStats =
				new List<TaskPerformanceStats>();

			writerMock.Setup( w => w.WriteAsync( It.IsAny<IEnumerable<TaskPerformanceStats>>() ) )
				.Callback<IEnumerable<TaskPerformanceStats>>( ws => actualWrittenStats.AddRange( ws ) );

			await perfMon.StartFlushingAsync( writerMock.Object );

			while ( perfStatsToReport.TryDequeue( out Tuple<string, long> execTime ) )
				await perfMon.ReportExecutionTimeAsync( execTime.Item1, execTime.Item2, 0 );

			await perfMon.StopFlushingAsync();

			CollectionAssert.AreEquivalent( expectedWrittenStats,
				actualWrittenStats );
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
		public async Task Test_CanReportExecutionStats_ParallelCalls ( int nReports, int nProducers )
		{

			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> perfStatsToReport = GenerateSamplePerformanceStats( nReports,
				out List<TaskPerformanceStats> expectedWrittenStats );

			Mock<IExecutionPerformanceMonitorWriter> writerMock =
				new Mock<IExecutionPerformanceMonitorWriter>();

			ConcurrentBag<TaskPerformanceStats> actualWrittenStats =
				new ConcurrentBag<TaskPerformanceStats>();

			Task[] producers = new Task[ nProducers ];

			writerMock.Setup( w => w.WriteAsync( It.IsAny<IEnumerable<TaskPerformanceStats>>() ) )
				.Callback<IEnumerable<TaskPerformanceStats>>( ws =>
				{
					foreach ( TaskPerformanceStats s in ws )
						actualWrittenStats.Add( s );
				} );

			await perfMon.StartFlushingAsync( writerMock.Object );

			for ( int i = 0; i < nProducers; i++ )
			{
				producers[ i ] = Task.Run( async () =>
				 {
					 while ( perfStatsToReport.TryDequeue( out Tuple<string, long> execTime ) )
						 await perfMon.ReportExecutionTimeAsync( execTime.Item1, execTime.Item2, 0 );
				 } );
			}

			await Task.WhenAll( producers );
			await perfMon.StopFlushingAsync();

			CollectionAssert.AreEquivalent( expectedWrittenStats.ToArray(),
				actualWrittenStats.ToArray() );
		}

		private ConcurrentQueue<Tuple<string, long>> GenerateSamplePerformanceStats ( int count,
			out List<TaskPerformanceStats> expectedWrittenStats )
		{
			Faker faker =
				new Faker();

			Type[] payloadTypes = new Type[]
			{
				typeof(AnotherSampleTaskPayload),
				typeof(ErroredTaskPayload),
				typeof(ImplicitSuccessfulTaskPayload),
				typeof(SampleNoExecutorPayload),
				typeof(SuccessfulTaskPayload),
				typeof(ThrowsExceptionTaskPayload)
			};

			ConcurrentQueue<Tuple<string, long>> execTimes =
				new ConcurrentQueue<Tuple<string, long>>();

			expectedWrittenStats =
				new List<TaskPerformanceStats>();

			for ( int i = 0; i < count; i++ )
			{
				long time = faker.Random.Long( 1, 10000 );
				Type taskType = faker.PickRandom( payloadTypes );

				execTimes.Enqueue( new Tuple<string, long>( 
					taskType.FullName, 
					time ) );

				expectedWrittenStats.Add( new TaskPerformanceStats( 
					taskType.FullName,
					time ) );
			}

			return execTimes;
		}
	}
}
