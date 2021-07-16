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
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor.SeviceModel;

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
			Console.WriteLine( "Performing processing result validation..." );

			int totalFileCount = sourceFileRepository.TotalFileCount;

			await CheckUsingTaskQueueInfoAsync( totalFileCount );
			await CheckUsingGenericCountsAsync( totalFileCount );
			await CheckUsingPayloadCountsAsync( GetExpectedPayloadCounts( totalFileCount ),
				GetExpectedRemainingPayloadCounts( totalFileCount ) );

			Console.WriteLine();
			Console.WriteLine( "Processing result validation completed." );
			Console.WriteLine();
		}

		private Dictionary<string, long> GetExpectedPayloadCounts( long totalFileCount )
		{
			return new Dictionary<string, long>()
			{
				{ typeof(HashFileByHandle).FullName, totalFileCount }
			};
		}

		private Dictionary<string, long> GetExpectedRemainingPayloadCounts( long totalFileCount )
		{
			return new Dictionary<string, long>()
			{
				{ typeof(HashFileByHandle).FullName, 0 }
			};
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
				WriteFailMessage( baseMessage );
			else
				WritePassMessage( baseMessage );
		}

		private async Task CheckUsingGenericCountsAsync( int expectedTotalCompletedTasks )
		{
			Console.WriteLine();
			Console.WriteLine( "Checking using generic counts..." );

			GenericCounts genericCounts = await mStatsProvider
				.ComputeGenericCountsAsync();

			CheckGenericTasksCount( genericCounts,
				expectedTotalRemainingTasks: 0 );
			CheckGenericResultsCount( genericCounts,
				expectedTotalCompletedTasks );
			CheckGenericCompletedResultsCount( genericCounts,
				expectedTotalCompletedTasks );
		}

		private void CheckGenericTasksCount( GenericCounts genericCounts, int expectedTotalRemainingTasks )
		{
			string baseRemainingTasksInQueueMessage = string.Format( "Expected remaining tasks in queue = {0}; actual = {1}",
				expectedTotalRemainingTasks,
				genericCounts.TotalTasksInQueue );

			if ( genericCounts.TotalTasksInQueue != expectedTotalRemainingTasks )
				WriteFailMessage( baseRemainingTasksInQueueMessage );
			else
				WritePassMessage( baseRemainingTasksInQueueMessage );
		}

		private void CheckGenericResultsCount( GenericCounts genericCounts, int expectedTotalCompletedTasks )
		{
			string baseResultsInResultQueueMessage = string.Format( "Expected results in result queue = {0}; actual = {1}",
				expectedTotalCompletedTasks,
				genericCounts.TotalResultsInResultQueue );

			if ( genericCounts.TotalResultsInResultQueue != expectedTotalCompletedTasks )
				WriteFailMessage( baseResultsInResultQueueMessage );
			else
				WritePassMessage( baseResultsInResultQueueMessage );
		}

		private void CheckGenericCompletedResultsCount( GenericCounts genericCounts, int expectedTotalCompletedTasks )
		{
			string baseCompeltedResultsInResultQueueMessage = string.Format( "Expected completed results in result queue = {0}; actual = {1}",
				expectedTotalCompletedTasks,
				genericCounts.TotalCompletedResultsInResultsQueue );

			if ( genericCounts.TotalCompletedResultsInResultsQueue != expectedTotalCompletedTasks )
				WriteFailMessage( baseCompeltedResultsInResultQueueMessage );
			else
				WritePassMessage( baseCompeltedResultsInResultQueueMessage );
		}

		private void WritePassMessage( string message )
		{
			WriteWithColor( $"[Pass] {message}",
				ConsoleColor.Green );
		}

		private void WriteFailMessage( string message )
		{
			WriteWithColor( $"[Fail] {message}",
				ConsoleColor.Red );
		}

		private void WriteWithColor( string message, ConsoleColor color )
		{
			ConsoleColor prevColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine( message );
			Console.ForegroundColor = prevColor;
		}

		private async Task CheckUsingPayloadCountsAsync( Dictionary<string, long> expectedPayloadCounts, Dictionary<string, long> expectedRemainingPayloadCounts )
		{
			Console.WriteLine();
			Console.WriteLine( "Checking using payload counts..." );

			PayloadCounts payloadCounts = await mStatsProvider
				.ComputePayloadCountsAsync();

			CheckPayloadTasksCount( payloadCounts,
				expectedRemainingPayloadCounts );
			CheckPayloadResultsCount( payloadCounts,
				expectedPayloadCounts );
			CheckPayloadCompletedResultsCount( payloadCounts,
				expectedPayloadCounts );
		}

		private void CheckPayloadTasksCount( PayloadCounts genericCounts, Dictionary<string, long> expectedRemainingPayloadCounts )
		{
			foreach ( KeyValuePair<string, long> expectedRemainingPayloadCount in expectedRemainingPayloadCounts )
			{
				long actualCount;
				if ( !genericCounts.TotalTasksInQueuePerPayload.TryGetValue( expectedRemainingPayloadCount.Key, out actualCount ) )
					actualCount = 0;

				string baseRemainingPayloadTasksInQueueMessage = string.Format( "Expected remaining tasks for payload {0} in queue = {1}; actual = {2}",
					FormatPayloadName( expectedRemainingPayloadCount.Key ),
					expectedRemainingPayloadCount.Value,
					actualCount );

				if ( expectedRemainingPayloadCount.Value != actualCount )
					WriteFailMessage( baseRemainingPayloadTasksInQueueMessage );
				else
					WritePassMessage( baseRemainingPayloadTasksInQueueMessage );
			}
		}

		private void CheckPayloadResultsCount( PayloadCounts genericCounts, Dictionary<string, long> expectedPayloadCounts )
		{
			foreach ( KeyValuePair<string, long> expectedPayloadCount in expectedPayloadCounts )
			{
				long actualCount;
				if ( !genericCounts.TotalResultsInResultQueuePerPayload.TryGetValue( expectedPayloadCount.Key, out actualCount ) )
					actualCount = 0;

				string baseRemainingResultsTasksInResultsQueueMessage = string.Format( "Expected remaining results for payload {0} in queue = {1}; actual = {2}",
					FormatPayloadName( expectedPayloadCount.Key ),
					expectedPayloadCount.Value,
					actualCount );

				if ( expectedPayloadCount.Value != actualCount )
					WriteFailMessage( baseRemainingResultsTasksInResultsQueueMessage );
				else
					WritePassMessage( baseRemainingResultsTasksInResultsQueueMessage );
			}
		}

		private void CheckPayloadCompletedResultsCount( PayloadCounts genericCounts, Dictionary<string, long> expectedPayloadCounts )
		{
			foreach ( KeyValuePair<string, long> expectedPayloadCount in expectedPayloadCounts )
			{
				long actualCount;
				if ( !genericCounts.TotalCompletedResultsInResultsQueuePerPayload.TryGetValue( expectedPayloadCount.Key, out actualCount ) )
					actualCount = 0;

				string baseRemainingCompletedResultsTasksInResultsQueueMessage = string.Format( "Expected remaining completed results for payload {0} in queue = {1}; actual = {2}",
					FormatPayloadName( expectedPayloadCount.Key ),
					expectedPayloadCount.Value,
					actualCount );

				if ( expectedPayloadCount.Value != actualCount )
					WriteFailMessage( baseRemainingCompletedResultsTasksInResultsQueueMessage );
				else
					WritePassMessage( baseRemainingCompletedResultsTasksInResultsQueueMessage );
			}
		}

		private string FormatPayloadName( string payloadName )
		{
			return payloadName.Substring( payloadName.LastIndexOf( '.' ) + 1 );
		}
	}
}
