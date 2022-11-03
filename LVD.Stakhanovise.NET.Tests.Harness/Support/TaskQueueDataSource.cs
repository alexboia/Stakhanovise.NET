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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using Npgsql;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TaskQueueDataSource
	{
		private string mConnectionString;

		private int mNumUnProcessedTasks = 50;

		private int mNumErroredTasks = 10;

		private int mNumFaultedTasks = 15;

		private int mNumFatalTasks = 1;

		private int mNumProcessedTasks = 10;

		private int mNumProcessingTasks = 5;

		private List<QueuedTask> mSeededTasks =
			new List<QueuedTask>();

		private List<QueuedTaskResult> mSeededTaskResults =
			new List<QueuedTaskResult>();

		private List<IQueuedTaskToken> mSeededTaskTokens =
			new List<IQueuedTaskToken>();

		private QueuedTaskMapping mMapping;

		private int mQueueFaultErrorThrehsoldCount;

		private DateTimeOffset mLastPostedAt;

		private TaskQueueDbOperations mOperations;

		public TaskQueueDataSource ( string connectionString,
			QueuedTaskMapping mapping,
			int queueFaultErrorThrehsoldCount )
		{
			mConnectionString = connectionString;
			mMapping = mapping;
			mQueueFaultErrorThrehsoldCount = queueFaultErrorThrehsoldCount;
			mOperations = new TaskQueueDbOperations( connectionString, mapping );
		}

		public int CountTasksOfTypeInQueue ( Type testType )
		{
			return mSeededTaskTokens.Count( t
				=> CanAddTaskToQueue( t.LastQueuedTaskResult )
				&& t.DequeuedTask.GetType().Equals( testType ) );
		}

		public async Task SeedData ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> allTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			mLastPostedAt = DateTimeOffset.UtcNow;

			allTasks.AddRange( GenerateFaultedTasks() );
			allTasks.AddRange( GenerateFataledTasks() );
			allTasks.AddRange( GenerateErroredTasks() );
			allTasks.AddRange( GenerateUnprocessedTasks() );
			allTasks.AddRange( GenerateProcessedTasks() );

			allTasks = allTasks
				.OrderBy( t => t.Item1.LockedUntilTs )
				.ToList();

			await InsertTaskDataAsync( allTasks );
		}

		public async Task ClearData ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				mSeededTasks.Clear();
				mSeededTaskResults.Clear();
				mSeededTaskTokens.Clear();
				await mOperations.ClearTaskAndResultDataAsync();
			}
		}

		public IQueuedTaskToken GetOriginalTokenData ( Guid taskId )
		{
			return mSeededTaskTokens.FirstOrDefault( t => t.DequeuedTask.Id == taskId );
		}

		public async Task<QueuedTask> GetQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			return await mOperations.GetQueuedTaskFromDbByIdAsync( taskId );
		}

		public async Task<QueuedTaskResult> GetQueuedTaskResultFromDbByIdAsync ( Guid taskId )
		{
			return await mOperations.GetQueuedTaskResultFromDbByIdAsync( taskId );
		}

		public async Task RemoveQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			await mOperations.RemoveQueuedTaskFromDbByIdAsync( taskId );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( mConnectionString );
			await db.OpenAsync();
			return db;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateUnprocessedTasks ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> unprocessedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumUnProcessedTasks; i++ )
			{
				mLastPostedAt = mLastPostedAt.AddMilliseconds( 100 );
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumUnProcessedTasks ),
					PostedAtTs = mLastPostedAt,
					Source = GetType().FullName,
					LockedUntilTs = mLastPostedAt,
					Priority = 0
				};

				unprocessedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
				) );
			}

			return unprocessedTasks;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateErroredTasks ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> erroredTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumErroredTasks; i++ )
			{
				mLastPostedAt = mLastPostedAt.AddMilliseconds( 100 );
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ErroredTaskPayload ).FullName,
					Payload = new ErroredTaskPayload(),
					PostedAtTs = mLastPostedAt,
					Source = GetType().FullName,
					LockedUntilTs = mLastPostedAt,
					Priority = 0
				};

				erroredTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastErrorIsRecoverable = i % 2 == 0,
						LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: error" ) ),
						ErrorCount = Math.Abs( mQueueFaultErrorThrehsoldCount - i ),
						Status = QueuedTaskStatus.Error
					}
				) );
			}

			return erroredTasks;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateFataledTasks ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> fataledTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumFatalTasks; i++ )
			{
				mLastPostedAt = mLastPostedAt.AddMilliseconds( 100 );
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ThrowsExceptionTaskPayload ).FullName,
					Payload = new ThrowsExceptionTaskPayload(),
					PostedAtTs = mLastPostedAt,
					Source = GetType().FullName,
					LockedUntilTs = mLastPostedAt,
					Priority = 0
				};

				fataledTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Fatal,
						FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastErrorIsRecoverable = i % 2 == 0,
						LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: fatal" ) ),
						ErrorCount = mQueueFaultErrorThrehsoldCount + i
					}
				) );
			}

			return fataledTasks;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateFaultedTasks ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> faultedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumFaultedTasks; i++ )
			{
				mLastPostedAt = mLastPostedAt.AddMilliseconds( 100 );
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ErroredTaskPayload ).FullName,
					Payload = new ErroredTaskPayload(),
					PostedAtTs = mLastPostedAt,
					Source = GetType().FullName,
					LockedUntilTs = mLastPostedAt,
					Priority = 0
				};

				faultedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Faulted,
						FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastErrorIsRecoverable = i % 2 == 0,
						LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: faulted" ) ),
						ErrorCount = mQueueFaultErrorThrehsoldCount
					}
				) );
			}

			return faultedTasks;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateProcessedTasks ()
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> processedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumProcessedTasks; i++ )
			{
				mLastPostedAt = mLastPostedAt.AddMilliseconds( 100 );
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SuccessfulTaskPayload ).FullName,
					Payload = new SuccessfulTaskPayload(),
					PostedAtTs = mLastPostedAt,
					Source = GetType().FullName,
					LockedUntilTs = mLastPostedAt,
					Priority = 0
				};

				processedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Processed,
						ProcessingTimeMilliseconds = 1000,
						FirstProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						LastProcessingAttemptedAtTs = DateTimeOffset.UtcNow,
						ProcessingFinalizedAtTs = DateTimeOffset.UtcNow,
						LastErrorIsRecoverable = false,
						LastError = null,
						ErrorCount = 0,
					}
				) );
			}

			return processedTasks;
		}

		private bool CanAddTaskToQueue ( IQueuedTaskResult result )
		{
			return result.Status != QueuedTaskStatus.Cancelled
				&& result.Status != QueuedTaskStatus.Processing
				&& result.Status != QueuedTaskStatus.Processed
				&& result.Status != QueuedTaskStatus.Fatal;
		}

		private bool CanAddTaskToQueue ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair )
		{
			return CanAddTaskToQueue( queuedTaskPair.Item2 );
		}

		private async Task InsertTaskDataAsync ( IEnumerable<Tuple<QueuedTask, QueuedTaskResult>> queuedTasks )
		{
			List<Tuple<QueuedTask, QueuedTaskResult>> queuedTaskToInsert =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();


			foreach ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair in queuedTasks )
			{
				if ( CanAddTaskToQueue( queuedTaskPair ) )
					queuedTaskToInsert.Add( queuedTaskPair );
				else
					queuedTaskToInsert.Add( new Tuple<QueuedTask, QueuedTaskResult>( null, queuedTaskPair.Item2 ) );

				mSeededTasks.Add( queuedTaskPair.Item1 );
				mSeededTaskResults.Add( queuedTaskPair.Item2 );

				mSeededTaskTokens.Add( new MockQueuedTaskToken(
					queuedTaskPair.Item1,
					queuedTaskPair.Item2 ) );
			}

			await mOperations.InsertTaskAndResultDataAsync( queuedTaskToInsert );
		}

		private bool CanTaskBeReposted ( IQueuedTaskToken token )
		{
			return token.LastQueuedTaskResult.Status != QueuedTaskStatus.Fatal
				&& token.LastQueuedTaskResult.Status != QueuedTaskStatus.Faulted
				&& token.LastQueuedTaskResult.Status != QueuedTaskStatus.Cancelled
				&& token.LastQueuedTaskResult.Status != QueuedTaskStatus.Processed;
		}

		public IEnumerable<QueuedTask> SeededTasks
			=> mSeededTasks.AsReadOnly();

		public IEnumerable<Type> InQueueTaskTypes
			=> mSeededTaskTokens
				.Where( t => CanAddTaskToQueue( t.LastQueuedTaskResult ) )
				.Select( t => t.DequeuedTask.Payload.GetType() )
				.Distinct()
				.AsEnumerable();

		public int NumTasksInQueue
			=> mSeededTaskTokens.Count( t => CanAddTaskToQueue( t.LastQueuedTaskResult ) );

		public IEnumerable<QueuedTaskResult> SeededTaskResults
			=> mSeededTaskResults.AsReadOnly();

		public IEnumerable<IQueuedTaskToken> SeededTaskTokens
			=> mSeededTaskTokens.AsReadOnly();

		public IEnumerable<IQueuedTaskToken> CanBeRepostedSeededTaskTokens
			=> mSeededTaskTokens
				.Where( t => CanTaskBeReposted( t ) )
				.AsEnumerable();

		public int NumUnProcessedTasks
			=> mNumUnProcessedTasks;

		public int NumErroredTasks
			=> mNumErroredTasks;

		public int NumFaultedTasks
			=> mNumFaultedTasks;

		public int NumFatalTasks
			=> mNumFatalTasks;

		public int NumProcessedTasks
			=> mNumProcessedTasks;

		public int NumProcessingTasks
			=> mNumProcessingTasks;

		public DateTimeOffset LastPostedAt
			=> mLastPostedAt;

		public DateTimeOffset MaxLockedUntilTs
			=> mSeededTasks.Max( t => t.LockedUntilTs );

		public int QueueFaultErrorThresholdCount
			=> mQueueFaultErrorThrehsoldCount;
	}
}
