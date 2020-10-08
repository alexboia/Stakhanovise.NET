using LVD.Stakhanovise.NET.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Helpers;
using System.Threading.Tasks;
using SqlKata;
using SqlKata.Execution;
using SqlKata.Compilers;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Helpers;
using LVD.Stakhanovise.NET.Queue;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class PostgreSqlTaskQueueDataSource
	{
		private string mConnectionString;

		private int mNumUnProcessedTasks = 5;

		private int mNumErroredTasks = 3;

		private int mNumFaultedTasks = 5;

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

		private long mLastPostedAtTimeTick = 1;

		public PostgreSqlTaskQueueDataSource ( string connectionString,
			QueuedTaskMapping mapping,
			int queueFaultErrorThrehsoldCount )
		{
			mConnectionString = connectionString;
			mMapping = mapping;
			mQueueFaultErrorThrehsoldCount = queueFaultErrorThrehsoldCount;
		}

		public int CountTasksOfTypeInQueue ( Type testType )
		{
			return mSeededTaskTokens.Count( t
				=> CanAddTaskToQueue( t.LastQueuedTaskResult )
				&& t.DequeuedTask.GetType().Equals( testType ) );
		}

		public async Task SeedData ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			using ( NpgsqlTransaction tx = db.BeginTransaction() )
			{
				List<Tuple<QueuedTask, QueuedTaskResult>> faultedTasks = GenerateFaultedTasks();
				await Task.Delay( 100 );
				List<Tuple<QueuedTask, QueuedTaskResult>> fataledTasks = GenerateFataledTasks();
				await Task.Delay( 100 );
				List<Tuple<QueuedTask, QueuedTaskResult>> erroredTasks = GenerateErroredTasks();
				await Task.Delay( 100 );
				List<Tuple<QueuedTask, QueuedTaskResult>> unprocessedTasks = GenerateUnprocessedTasks();
				await Task.Delay( 100 );
				List<Tuple<QueuedTask, QueuedTaskResult>> processedTasks = GenerateProcessedTasks();
				await Task.Delay( 100 );

				await InsertTaskDataAsync( unprocessedTasks, db, tx );
				await InsertTaskDataAsync( erroredTasks, db, tx );
				await InsertTaskDataAsync( fataledTasks, db, tx );
				await InsertTaskDataAsync( faultedTasks, db, tx );
				await InsertTaskDataAsync( processedTasks, db, tx );

				await tx.CommitAsync();
			}
		}

		public async Task ClearData ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				mSeededTasks.Clear();
				mSeededTaskResults.Clear();
				mSeededTaskTokens.Clear();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.DeleteAsync();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.DeleteAsync();
			}
		}

		public IQueuedTaskToken GetOriginalTokenData(Guid taskId)
		{
			return mSeededTaskTokens.FirstOrDefault( t => t.DequeuedTask.Id == taskId );
		}

		public async Task<QueuedTask> GetQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Select( "*" )
					.Where( "task_id", "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskAsync()
						: null;
			}
		}

		public async Task<QueuedTaskResult> GetQueuedTaskResultFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.Select( "*" )
					.Where( "task_id", "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskResultAsync()
						: null;
			}
		}

		public async Task RemoveQueuedTaskFromDbByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				await new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Where( "task_id", "=", taskId )
					.DeleteAsync();
			}
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( mConnectionString );
			await db.OpenAsync();
			return db;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateUnprocessedTasks ()
		{
			DateTimeOffset now = DateTimeOffset.Now;
			List<Tuple<QueuedTask, QueuedTaskResult>> unprocessedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumUnProcessedTasks; i++ )
			{
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumUnProcessedTasks ),
					PostedAtTs = now,
					Source = GetType().FullName,
					PostedAt = mLastPostedAtTimeTick++,
					LockedUntil = 1,
					Priority = 0
				};


				unprocessedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Unprocessed
					}
				) );
			}

			return unprocessedTasks;
		}

		private List<Tuple<QueuedTask, QueuedTaskResult>> GenerateErroredTasks ()
		{
			DateTimeOffset now = DateTimeOffset.Now;
			List<Tuple<QueuedTask, QueuedTaskResult>> erroredTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumErroredTasks; i++ )
			{
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ErroredTaskPayload ).FullName,
					Payload = new ErroredTaskPayload(),
					PostedAtTs = now,
					Source = GetType().FullName,
					PostedAt = mLastPostedAtTimeTick++,
					LockedUntil = 1,
					Priority = 0
				};

				erroredTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
						LastProcessingAttemptedAtTs = DateTimeOffset.Now,
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
			DateTimeOffset now = DateTimeOffset.Now;
			List<Tuple<QueuedTask, QueuedTaskResult>> fataledTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumFatalTasks; i++ )
			{
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ThrowsExceptionTaskPayload ).FullName,
					Payload = new ThrowsExceptionTaskPayload(),
					PostedAtTs = now,
					Source = GetType().FullName,
					PostedAt = mLastPostedAtTimeTick++,
					LockedUntil = 1,
					Priority = 0
				};

				fataledTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Fatal,
						FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
						LastProcessingAttemptedAtTs = DateTimeOffset.Now,
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
			DateTimeOffset now = DateTimeOffset.Now;
			List<Tuple<QueuedTask, QueuedTaskResult>> faultedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumFaultedTasks; i++ )
			{
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( ErroredTaskPayload ).FullName,
					Payload = new ErroredTaskPayload(),
					PostedAtTs = now,
					Source = GetType().FullName,
					PostedAt = mLastPostedAtTimeTick++,
					LockedUntil = 1,
					Priority = 0
				};

				faultedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Faulted,
						FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
						LastProcessingAttemptedAtTs = DateTimeOffset.Now,
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
			DateTimeOffset now = DateTimeOffset.Now;
			List<Tuple<QueuedTask, QueuedTaskResult>> processedTasks =
				new List<Tuple<QueuedTask, QueuedTaskResult>>();

			for ( int i = 0; i < mNumProcessedTasks; i++ )
			{
				QueuedTask task = new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SuccessfulTaskPayload ).FullName,
					Payload = new SuccessfulTaskPayload(),
					PostedAtTs = now,
					Source = GetType().FullName,
					PostedAt = mLastPostedAtTimeTick++,
					LockedUntil = 1,
					Priority = 0
				};

				processedTasks.Add( new Tuple<QueuedTask, QueuedTaskResult>(
					task,
					new QueuedTaskResult( task )
					{
						Status = QueuedTaskStatus.Processed,
						ProcessingTimeMilliseconds = 1000,
						FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
						LastProcessingAttemptedAtTs = DateTimeOffset.Now,
						ProcessingFinalizedAtTs = DateTimeOffset.Now,
						LastErrorIsRecoverable = false,
						LastError = null,
						ErrorCount = 0,
					}
				) );
			}

			return processedTasks;
		}

		private List<QueuedTask> GenerateProcessingTasks ( DateTimeOffset now )
		{
			List<QueuedTask> processedTasks = new List<QueuedTask>();

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

		private async Task AddTaskToQueue ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTask = new Dictionary<string, object>()
			{
				{ "task_id",
					queuedTaskPair.Item1.Id },
				{ "task_payload",
					queuedTaskPair.Item1.Payload.ToJson(includeTypeInformation: true) },
				{ "task_type",
					queuedTaskPair.Item1.Type },

				{ "task_source",
					queuedTaskPair.Item1.Source },
				{ "task_priority",
					queuedTaskPair.Item1.Priority },

				{ "task_posted_at",
					queuedTaskPair.Item1.PostedAt },
				{ "task_posted_at_ts",
					queuedTaskPair.Item1.PostedAtTs },

				{ "task_locked_until",
					queuedTaskPair.Item1.LockedUntil }
			};

			await new QueryFactory( conn, new PostgresCompiler() )
				.Query( mMapping.QueueTableName )
				.InsertAsync( insertDataTask, tx );
		}

		private async Task AddTaskResultToResultQueue ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTaskResult = new Dictionary<string, object>()
			{
				{ "task_id",
					queuedTaskPair.Item2.Id },
				{ "task_payload",
					queuedTaskPair.Item1.Payload.ToJson(includeTypeInformation: true) },
				{ "task_type",
					queuedTaskPair.Item1.Type },

				{ "task_source",
					queuedTaskPair.Item1.Source },
				{ "task_priority",
					queuedTaskPair.Item1.Priority },

				{ "task_posted_at",
					queuedTaskPair.Item1.PostedAt },
				{ "task_posted_at_ts",
					queuedTaskPair.Item1.PostedAtTs },

				{ "task_status",
					queuedTaskPair.Item2.Status },

				{ "task_processing_time_milliseconds",
					queuedTaskPair.Item2.ProcessingTimeMilliseconds },

				{ "task_error_count",
					queuedTaskPair.Item2.ErrorCount },
				{ "task_last_error",
					queuedTaskPair.Item2.LastError.ToJson() },
				{ "task_last_error_is_recoverable",
					queuedTaskPair.Item2.LastErrorIsRecoverable },

				{ "task_first_processing_attempted_at_ts",
					queuedTaskPair.Item2.FirstProcessingAttemptedAtTs },
				{ "task_last_processing_attempted_at_ts",
					queuedTaskPair.Item2.LastProcessingAttemptedAtTs },
				{ "task_processing_finalized_at_ts",
					queuedTaskPair.Item2.ProcessingFinalizedAtTs }
			};

			await new QueryFactory( conn, new PostgresCompiler() )
				.Query( mMapping.ResultsQueueTableName )
				.InsertAsync( insertDataTaskResult, tx );
		}

		private async Task InsertTaskDataAsync ( IEnumerable<Tuple<QueuedTask, QueuedTaskResult>> queuedTasks,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			foreach ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair in queuedTasks )
			{
				if ( CanAddTaskToQueue( queuedTaskPair ) )
					await AddTaskToQueue( queuedTaskPair, conn, tx );

				await AddTaskResultToResultQueue( queuedTaskPair, conn, tx );

				mSeededTasks.Add( queuedTaskPair.Item1 );
				mSeededTaskResults.Add( queuedTaskPair.Item2 );

				mSeededTaskTokens.Add( new MockQueuedTaskToken(
					queuedTaskPair.Item1,
					queuedTaskPair.Item2 ) );
			}
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

		public AbstractTimestamp LastPostedAt
			=> new AbstractTimestamp( mLastPostedAtTimeTick,
				mLastPostedAtTimeTick * 100 );

		public long LastPostedAtTimeTick
			=> mLastPostedAtTimeTick;

		public int QueueFaultErrorThresholdCount
			=> mQueueFaultErrorThrehsoldCount;
	}
}
