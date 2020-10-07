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

				await InsertTaskDataAsync( db, unprocessedTasks, tx );
				await InsertTaskDataAsync( db, erroredTasks, tx );
				await InsertTaskDataAsync( db, fataledTasks, tx );
				await InsertTaskDataAsync( db, faultedTasks, tx );
				await InsertTaskDataAsync( db, processedTasks, tx );

				await tx.CommitAsync();
			}
		}

		public async Task ClearData ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				mSeededTasks.Clear();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.DeleteAsync();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsTableName )
					.DeleteAsync();
			}
		}

		public async Task<QueuedTask> GetQueuedTaskByIdAsync ( Guid taskId )
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

		public async Task<QueuedTaskResult> GetQueuedTaskResultByIdAsync ( Guid taskId )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				Query selectTaskQuery = new QueryFactory( db, new PostgresCompiler() )
					.Query( mMapping.ResultsTableName )
					.Select( "*" )
					.Where( "task_id", "=", taskId );

				using ( NpgsqlDataReader reader = await db.ExecuteReaderAsync( selectTaskQuery ) )
					return await reader.ReadAsync()
						? await reader.ReadQueuedTaskResultAsync()
						: null;
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
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumErroredTasks ),
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
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFatalTasks ),
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
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFaultedTasks ),
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
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumProcessedTasks ),
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

		private async Task InsertTaskDataAsync ( NpgsqlConnection conn,
			IEnumerable<Tuple<QueuedTask, QueuedTaskResult>> queuedTasks,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTask;
			Dictionary<string, object> insertDataTaskResult;

			foreach ( Tuple<QueuedTask, QueuedTaskResult> queuedTaskPair in queuedTasks )
			{
				if ( queuedTaskPair.Item2.Status != QueuedTaskStatus.Cancelled
					&& queuedTaskPair.Item2.Status != QueuedTaskStatus.Processing
					&& queuedTaskPair.Item2.Status != QueuedTaskStatus.Processed
					&& queuedTaskPair.Item2.Status != QueuedTaskStatus.Fatal )
				{
					insertDataTask = new Dictionary<string, object>()
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

				insertDataTaskResult = new Dictionary<string, object>()
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
					.Query( mMapping.ResultsTableName )
					.InsertAsync( insertDataTaskResult, tx );

				mSeededTasks.Add( queuedTaskPair.Item1 );
				mSeededTaskResults.Add( queuedTaskPair.Item2 );

				mSeededTaskTokens.Add( new MockQueuedTaskToken(
					queuedTaskPair.Item1,
					queuedTaskPair.Item2 ) );
			}
		}

		public IEnumerable<QueuedTask> SeededTasks
			=> mSeededTasks.AsReadOnly();

		public IEnumerable<QueuedTaskResult> SeededTaskResults
			=> mSeededTaskResults.AsReadOnly();

		public IEnumerable<IQueuedTaskToken> SeededTaskTokens
			=> mSeededTaskTokens.AsReadOnly();

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
			=> new AbstractTimestamp( mLastPostedAtTimeTick, mLastPostedAtTimeTick * 100 );

		public long LastPostedAtTimeTick
			=> mLastPostedAtTimeTick;

		public int QueueFaultErrorThresholdCount
			=> mQueueFaultErrorThrehsoldCount;
	}
}
