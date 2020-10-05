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
		[TestCase( 15 )]
		[TestCase( 50 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		[Repeat( 5 )]
		public void Test_CanReportExecutionTime_NoFlush_SerialCalls ( int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> execTimesToReport = GenerateExecutionTimesToReport( nReports,
				out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals );

			while ( execTimesToReport.TryDequeue( out Tuple<string, long> execTime ) )
				perfMon.ReportExecutionTime( execTime.Item1, execTime.Item2 );

			Assert_CorrectStats( perfMon, expectedPerPayloadTotals,
				validateLastExecutionTime: true );
		}

		[Test]
		[TestCase( 2, 1 )]
		[TestCase( 2, 5 )]
		[TestCase( 2, 10 )]
		[TestCase( 2, 15 )]
		[TestCase( 2, 50 )]
		[TestCase( 2, 100 )]
		[TestCase( 2, 1000 )]

		[TestCase( 5, 1 )]
		[TestCase( 5, 5 )]
		[TestCase( 5, 10 )]
		[TestCase( 5, 15 )]
		[TestCase( 5, 50 )]
		[TestCase( 5, 100 )]
		[TestCase( 5, 1000 )]
		[Repeat( 5 )]
		public void Test_CanReportExecutionTime_NoFlush_ParallelCalls ( int nThreads, int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> execTimesToReport = GenerateExecutionTimesToReport( nReports,
				out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals );

			Task[] threads = new Task[ nThreads ];
			Barrier syncStart = new Barrier( nThreads );

			for ( int i = 0; i < nThreads; i++ )
			{
				threads[ i ] = Task.Run( () =>
				{
					syncStart.SignalAndWait();
					while ( execTimesToReport.TryDequeue( out Tuple<string, long> execTime ) )
						perfMon.ReportExecutionTime( execTime.Item1, execTime.Item2 );
				} );
			}

			Task.WaitAll( threads );

			Assert_CorrectStats( perfMon,
				expectedPerPayloadTotals,
				validateLastExecutionTime: false );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public async Task Test_CanReportExecutionTime_WithFlush_WriteCountThreshold ( int writeCountTheshold )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> execTimesToReport = GenerateExecutionTimesToReport( writeCountTheshold,
				out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals );

			ExecutionPerformanceMonitorWriteOptions writeOptions = new ExecutionPerformanceMonitorWriteOptions( writeIntervalThresholdMilliseconds: 1000,
				writeCountTheshold );

			await Run_FlushTests( perfMon,
				execTimesToReport,
				expectedPerPayloadTotals,
				writeOptions );
		}


		[Test]
		[TestCase( 100, 5 )]
		[TestCase( 100, 1 )]
		[TestCase( 100, 10 )]
		[TestCase( 1500, 5 )]
		[TestCase( 1500, 1 )]
		[TestCase( 1500, 10 )]
		[Repeat( 10 )]
		public async Task Test_CanReportExecutionTime_WithFlush_WriteIntervalThreshold ( int writeIntervalThreshold, int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			ConcurrentQueue<Tuple<string, long>> execTimesToReport = GenerateExecutionTimesToReport( nReports,
				out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals );

			ExecutionPerformanceMonitorWriteOptions writeOptions = new ExecutionPerformanceMonitorWriteOptions( writeIntervalThreshold,
				writeCountThreshold: nReports * 2 );

			await Run_FlushTests( perfMon,
				execTimesToReport,
				expectedPerPayloadTotals,
				writeOptions );
		}

		[Test]
		[TestCase( 100 )]
		[TestCase( 250 )]
		[TestCase( 1000 )]
		[TestCase( 1500 )]
		[Repeat( 3 )]
		public async Task Test_FlushWithoutChanges_NoInitialReports ( int writeIntervalThreshold )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			Mock<IExecutionPerformanceMonitorWriter> writerMock =
				new Mock<IExecutionPerformanceMonitorWriter>();

			ExecutionPerformanceMonitorWriteOptions writeOptions = new ExecutionPerformanceMonitorWriteOptions( writeIntervalThreshold,
				writeCountThreshold: 1000 );

			CountdownEvent syncCount = new CountdownEvent( 3 );

			writerMock.Setup( w => w.SetupIfNeededAsync() );
			writerMock.Setup( w => w.WriteAsync( It.IsAny<IReadOnlyDictionary<string, TaskExecutionStats>>() ) )
				.Callback<IReadOnlyDictionary<string, TaskExecutionStats>>( f =>
				{
					try
					{
						Assert.NotNull( f );
						Assert.AreEqual( 0, f.Count );
					}
					finally
					{
						syncCount.Signal();
					}
				} );

			await perfMon.StartFlushingAsync( writerMock.Object, writeOptions );
			await perfMon.StopFlushingAsync();

			syncCount.Wait();
		}

		[Test]
		[TestCase( 100, 5 )]
		[TestCase( 100, 1 )]
		[TestCase( 100, 10 )]
		[TestCase( 1500, 5 )]
		[TestCase( 1500, 1 )]
		[TestCase( 1500, 10 )]
		public async Task Test_FlushWithoutChanges_WithInitialReports ( int writeIntervalThreshold, int nReports )
		{
			StandardExecutionPerformanceMonitor perfMon =
				new StandardExecutionPerformanceMonitor();

			Mock<IExecutionPerformanceMonitorWriter> writerMock =
				new Mock<IExecutionPerformanceMonitorWriter>();

			ConcurrentQueue<Tuple<string, long>> execTimesToReport = GenerateExecutionTimesToReport( nReports,
				out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals );

			ExecutionPerformanceMonitorWriteOptions writeOptions = new ExecutionPerformanceMonitorWriteOptions( writeIntervalThreshold,
				nReports * 2 );

			CountdownEvent syncCount = new CountdownEvent( 3 );
			IReadOnlyDictionary<string, TaskExecutionStats> flushedArgs = null;

			writerMock.Setup( w => w.SetupIfNeededAsync() );
			writerMock.Setup( w => w.WriteAsync( It.IsAny<IReadOnlyDictionary<string, TaskExecutionStats>>() ) )
				.Callback<IReadOnlyDictionary<string, TaskExecutionStats>>( f =>
				{
					try
					{
						Assert.NotNull( f );
						Assert.AreEqual( expectedPerPayloadTotals.Count, f.Count );

						if ( flushedArgs == null )
						{
							flushedArgs = f;
							Assert_CorrectStats( expectedPerPayloadTotals, f, validateLastExecutionTime: false );
						}
						else
							Assert_UnchangedStats( flushedArgs, f );
					}
					finally
					{
						syncCount.Signal();
					}
				} );

			await perfMon.StartFlushingAsync( writerMock.Object, writeOptions );

			while ( execTimesToReport.TryDequeue( out Tuple<string, long> execTime ) )
				perfMon.ReportExecutionTime( execTime.Item1, execTime.Item2 );

			await perfMon.StopFlushingAsync();

			syncCount.Wait();
		}

		private static async Task Run_FlushTests ( StandardExecutionPerformanceMonitor perfMon,
			ConcurrentQueue<Tuple<string, long>> execTimesToReport,
			Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals,
			ExecutionPerformanceMonitorWriteOptions writeOptions )
		{
			int execTimesToReportCount = execTimesToReport.Count;
			IReadOnlyDictionary<string, TaskExecutionStats> flushedArgs = null;

			Mock<IExecutionPerformanceMonitorWriter> writerMock =
				new Mock<IExecutionPerformanceMonitorWriter>();

			using ( ManualResetEvent syncFlushed = new ManualResetEvent( false ) )
			{
				writerMock.Setup( w => w.SetupIfNeededAsync() );
				writerMock.Setup( w => w.WriteAsync( It.IsAny<IReadOnlyDictionary<string, TaskExecutionStats>>() ) )
					.Callback<IReadOnlyDictionary<string, TaskExecutionStats>>( f =>
					{
						flushedArgs = f;
						syncFlushed.Set();
					} );

				await perfMon.StartFlushingAsync( writerMock.Object, writeOptions );

				while ( execTimesToReport.TryDequeue( out Tuple<string, long> execTime ) )
					perfMon.ReportExecutionTime( execTime.Item1, execTime.Item2 );

				await perfMon.StopFlushingAsync();
				syncFlushed.WaitOne();

				Assert.NotNull( flushedArgs );
				Assert.AreEqual( expectedPerPayloadTotals.Count, flushedArgs.Count );

				Assert_CorrectStats( expectedPerPayloadTotals,
					flushedArgs,
					validateLastExecutionTime: true );
			}
		}

		private static void Assert_UnchangedStats ( IReadOnlyDictionary<string, TaskExecutionStats> lastPerPayloadTotals,
			IReadOnlyDictionary<string, TaskExecutionStats> currentTotals )
		{
			foreach ( KeyValuePair<string, TaskExecutionStats> expected in lastPerPayloadTotals )
			{
				if ( currentTotals.TryGetValue( expected.Key, out TaskExecutionStats stats ) )
				{
					Assert.AreEqual( expected.Value.FastestExecutionTime, stats.FastestExecutionTime );
					Assert.AreEqual( expected.Value.LongestExecutionTime, stats.LongestExecutionTime );
					Assert.AreEqual( 0, stats.AverageExecutionTime );
					Assert.AreEqual( expected.Value.LastExecutionTime, stats.LastExecutionTime );
					Assert.AreEqual( 0, stats.NumberOfExecutionCycles );
					Assert.AreEqual( 0, stats.TotalExecutionTime );
				}
				else
					Assert.Fail();
			}
		}

		private static void Assert_CorrectStats ( StandardExecutionPerformanceMonitor perfMon,
			Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals,
			bool validateLastExecutionTime )
		{
			foreach ( KeyValuePair<string, TaskExecutionStats> expected in expectedPerPayloadTotals )
			{
				TaskExecutionStats stats = perfMon.GetExecutionStats( expected.Key );
				Assert.NotNull( stats );

				Assert.AreEqual( expected.Value.FastestExecutionTime, stats.FastestExecutionTime );
				Assert.AreEqual( expected.Value.LongestExecutionTime, stats.LongestExecutionTime );
				Assert.AreEqual( expected.Value.AverageExecutionTime, stats.AverageExecutionTime );
				if ( validateLastExecutionTime )
					Assert.AreEqual( expected.Value.LastExecutionTime, stats.LastExecutionTime );
				Assert.AreEqual( expected.Value.NumberOfExecutionCycles, stats.NumberOfExecutionCycles );
				Assert.AreEqual( expected.Value.TotalExecutionTime, stats.TotalExecutionTime );
			}
		}

		private static void Assert_CorrectStats ( Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals,
			IReadOnlyDictionary<string, TaskExecutionStats> actualTotals,
			bool validateLastExecutionTime )
		{
			foreach ( KeyValuePair<string, TaskExecutionStats> expected in expectedPerPayloadTotals )
			{
				if ( actualTotals.TryGetValue( expected.Key, out TaskExecutionStats stats ) )
				{
					Assert.AreEqual( expected.Value.FastestExecutionTime, stats.FastestExecutionTime );
					Assert.AreEqual( expected.Value.LongestExecutionTime, stats.LongestExecutionTime );
					Assert.AreEqual( expected.Value.AverageExecutionTime, stats.AverageExecutionTime );
					if ( validateLastExecutionTime )
						Assert.AreEqual( expected.Value.LastExecutionTime, stats.LastExecutionTime );
					Assert.AreEqual( expected.Value.NumberOfExecutionCycles, stats.NumberOfExecutionCycles );
					Assert.AreEqual( expected.Value.TotalExecutionTime, stats.TotalExecutionTime );
				}
				else
					Assert.Fail();
			}
		}

		private ConcurrentQueue<Tuple<string, long>> GenerateExecutionTimesToReport ( int nReports,
			out Dictionary<string, TaskExecutionStats> expectedPerPayloadTotals )
		{
			Faker faker = new Faker();
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

			expectedPerPayloadTotals = new Dictionary<string, TaskExecutionStats>();

			for ( int i = 0; i < nReports; i++ )
			{
				long time = faker.Random.Long( 1, 10000 );
				Type taskType = faker.PickRandom( payloadTypes );

				execTimes.Enqueue( new Tuple<string, long>( taskType.FullName, time ) );

				if ( expectedPerPayloadTotals.TryGetValue( taskType.FullName, out TaskExecutionStats stats ) )
					expectedPerPayloadTotals[ taskType.FullName ] =
						stats.UpdateWithNewCycleExecutionTime( time );
				else
					expectedPerPayloadTotals[ taskType.FullName ] =
						TaskExecutionStats.Initial( time );
			}

			return execTimes;
		}
	}
}
