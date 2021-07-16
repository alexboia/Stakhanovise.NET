// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
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
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Queue;
using Npgsql;
using LVD.Stakhanovise.NET.Samples.FileHasher.Statistics;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor;

namespace LVD.Stakhanovise.NET.Samples.FileHasher
{
	public class FileHasherAppResultValidator
	{
		private ITaskQueueInfo mTaskQueueInfo;

		private IStatsProvider mStatsProvider;

		//We need to combine multiple validation methods:
		// 1. Use public task queue info API to check that queue state 
		//		corresponds to the processing completed state
		// 2. Directly lookup queue tables (queue and results tables, respectively)
		//		for detailed checks: 
		//		- no records in queue table
		//		- expected record count in results table
		//		- task payloads are correct
		//		- task statuses are correct
		//	3. Lookup execution time stats and check that number of execution cycles is correct
		//	4. Lookup metrics table and check that the relevant metrics are correct:
		//		- listener@task-notification-count - this may be affected by potential connect dropouts,
		//			so also check listener@reconnect-count
		//		- poller@dequeue-count
		//		- queue-consumer@dequeue-count
		//		- worker@processed-payload-count

		public FileHasherAppResultValidator( ITaskQueueInfo taskQueueInfo, IStatsProvider statsProvider )
		{
			mTaskQueueInfo = taskQueueInfo
				?? throw new ArgumentNullException( nameof( taskQueueInfo ) );
			mStatsProvider = statsProvider
				?? throw new ArgumentNullException( nameof( statsProvider ) );
		}

		public async Task PerformBasicValidationAsync( ISourceFileRepository sourceFileRepository, IFileHashRepository fileHashRepository )
		{
			Console.WriteLine();
			Console.WriteLine( "Performing processing result validation..." );

			await CheckUsingTaskQueueInfoAsync( sourceFileRepository.TotalFileCount );
			await CheckUsingGenericCounts( sourceFileRepository.TotalFileCount );

			Console.WriteLine();
			Console.WriteLine( "Processing result validation completed." );
			Console.WriteLine();
		}

		private async Task CheckUsingTaskQueueInfoAsync( int expectedTotalCompletedTasks )
		{
			Console.WriteLine();
			Console.WriteLine( "Checking using built-in task queue info metrics..." );

			TaskQueueMetrics metrics = await mTaskQueueInfo
				.ComputeMetricsAsync();

			string baseMessage = string.Format( "Expected total processed = {0}; actual = {1}",
				expectedTotalCompletedTasks,
				metrics.TotalProcessed );

			if ( metrics.TotalProcessed != expectedTotalCompletedTasks )
				WriteWithColor( $"[Fail] {baseMessage}",
					ConsoleColor.Red );
			else
				WriteWithColor( $"[Pass] {baseMessage}",
					ConsoleColor.Green );
		}

		private async Task CheckUsingGenericCounts( int expectedTotalCompletedTasks )
		{
			Console.WriteLine();
			Console.WriteLine( "Checking using generic counts..." );

			GenericCounts genericCounts = await mStatsProvider
				.ComputeGenericCountsAsync();

			string baseRemainingTasksInQueueMessage = string.Format( "Expected remaining tasks in queue = 0; actual = {0}",
				genericCounts.TotalTasksInQueue );

			if ( genericCounts.TotalTasksInQueue != 0 )
				WriteWithColor( $"[Fail] {baseRemainingTasksInQueueMessage}",
					ConsoleColor.Red );
			else
				WriteWithColor( $"[Pass] {baseRemainingTasksInQueueMessage}",
					ConsoleColor.Green );

			string baseResultsInResultQueueMessage = string.Format( "Expected results in result queue = {0}; actual = {1}",
				expectedTotalCompletedTasks,
				genericCounts.TotalResultsInResultQueue );

			if ( genericCounts.TotalResultsInResultQueue != expectedTotalCompletedTasks )
				WriteWithColor( $"[Fail] {baseResultsInResultQueueMessage}",
					ConsoleColor.Red );
			else
				WriteWithColor( $"[Success] {baseResultsInResultQueueMessage}",
					ConsoleColor.Green );

			string baseCompeltedResultsInResultQueueMessage = string.Format( "Expected completed results in result queue = {0}; actual = {1}",
				expectedTotalCompletedTasks,
				genericCounts.TotalCompletedResultsInResultsQueue );

			if ( genericCounts.TotalCompletedResultsInResultsQueue != expectedTotalCompletedTasks )
				WriteWithColor( $"[Fail] {baseCompeltedResultsInResultQueueMessage}",
					ConsoleColor.Red );
			else
				WriteWithColor( $"[Success] {baseCompeltedResultsInResultQueueMessage}",
					ConsoleColor.Green );
		}

		private void WriteWithColor( string message, ConsoleColor color )
		{
			ConsoleColor prevColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine( message );
			Console.ForegroundColor = prevColor;
		}
	}
}
