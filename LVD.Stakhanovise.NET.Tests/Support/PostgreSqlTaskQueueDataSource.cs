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

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class PostgreSqlTaskQueueDataSource
	{
		private string mConnectionString;

		private int mNumUnProcessedTasks = 5;

		private int mNumErroredTasks = 3;

		private int mNumFaultedTasks = 5;

		private int mNumFatalTasks = 1;

		private List<QueuedTask> mSeededTasks =
			new List<QueuedTask>();

		private QueuedTaskMapping mMapping;

		private int mQueueFaultErrorThrehsoldCount;

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
				DateTimeOffset now = DateTimeOffset.Now;
				List<QueuedTask> faultedTasks = GenerateFaultedTasks( now );
				List<QueuedTask> fataledTasks = GenerateFataledTasks( now );
				List<QueuedTask> erroredTasks = GenerateErroredTasks( now );
				List<QueuedTask> unprocessedTasks = GenerateUnprocessedTasks( now );

				await InsertTaskDataAsync( db, unprocessedTasks, tx );
				await InsertTaskDataAsync( db, erroredTasks, tx );
				await InsertTaskDataAsync( db, fataledTasks, tx );
				await InsertTaskDataAsync( db, faultedTasks, tx );

				await tx.CommitAsync();
			}
		}

		public async Task ClearData ()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync() )
			{
				mSeededTasks.Clear();
				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.TableName )
					.DeleteAsync();
			}
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( mConnectionString );
			await db.OpenAsync();
			return db;
		}

		private List<QueuedTask> GenerateUnprocessedTasks ( DateTimeOffset now )
		{
			List<QueuedTask> unprocessedTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumUnProcessedTasks; i++ )
			{
				unprocessedTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumUnProcessedTasks ),
					PostedAtTs = now,
					RepostedAtTs = now,
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Unprocessed,
					PostedAt = 1,
					LockedUntil = 1,
					Priority = 0
				} );
			}

			return unprocessedTasks;
		}

		private List<QueuedTask> GenerateErroredTasks ( DateTimeOffset now )
		{
			List<QueuedTask> erroredTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumErroredTasks; i++ )
			{
				erroredTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumErroredTasks ),
					PostedAtTs = now.AddSeconds( 1 ),
					RepostedAtTs = now.AddSeconds( 1 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Error,
					FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: error" ) ),
					ErrorCount = Math.Abs( mQueueFaultErrorThrehsoldCount - i ),
					PostedAt = 1,
					LockedUntil = 1,
					Priority = 0
				} );
			}

			return erroredTasks;
		}

		private List<QueuedTask> GenerateFataledTasks ( DateTimeOffset now )
		{
			List<QueuedTask> fataledTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumFatalTasks; i++ )
			{
				fataledTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFatalTasks ),
					PostedAtTs = now.AddSeconds( 2 ),
					RepostedAtTs = now.AddSeconds( 2 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Fatal,
					FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: fatal" ) ),
					ErrorCount = mQueueFaultErrorThrehsoldCount + i,
					PostedAt = 1,
					LockedUntil = 1,
					Priority = 0
				} );
			}

			return fataledTasks;
		}

		private List<QueuedTask> GenerateFaultedTasks ( DateTimeOffset now )
		{
			List<QueuedTask> faultedTasks = new List<QueuedTask>();

			for ( int i = 0; i < mNumFaultedTasks; i++ )
			{
				faultedTasks.Add( new QueuedTask()
				{
					Id = Guid.NewGuid(),
					Type = typeof( SampleTaskPayload ).FullName,
					Payload = new SampleTaskPayload( mNumFaultedTasks ),
					PostedAtTs = now.AddSeconds( 3 ),
					RepostedAtTs = now.AddSeconds( 3 ),
					Source = GetType().FullName,
					Status = QueuedTaskStatus.Faulted,
					FirstProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastProcessingAttemptedAtTs = DateTimeOffset.Now,
					LastErrorIsRecoverable = i % 2 == 0,
					LastError = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception: faulted" ) ),
					ErrorCount = mQueueFaultErrorThrehsoldCount,
					PostedAt = 1,
					LockedUntil = 1,
					Priority = 0
				} );
			}

			return faultedTasks;
		}

		private async Task InsertTaskDataAsync ( NpgsqlConnection conn, IEnumerable<QueuedTask> queuedTasks, NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertData;

			foreach ( QueuedTask queuedTask in queuedTasks )
			{
				insertData = new Dictionary<string, object>()
					{
						{ mMapping.IdColumnName,
							queuedTask.Id },
						{ mMapping.PayloadColumnName,
							queuedTask.Payload.ToJson(includeTypeInformation: true) },
						{ mMapping.TypeColumnName,
							queuedTask.Type },

						{ mMapping.StatusColumnName,
							queuedTask.Status },
						{ mMapping.SourceColumnName,
							queuedTask.Source },
						{ mMapping.PriorityColumnName,
							queuedTask.Priority },

						{ mMapping.PostedAtColumnName,
							queuedTask.PostedAt },
						{ mMapping.LockedUntilColumnName,
							queuedTask.LockedUntil },

						{ mMapping.PostedAtTsColumnName,
							queuedTask.PostedAtTs },
						{ mMapping.RepostedAtTsColumnName,
							queuedTask.RepostedAtTs },
						{ mMapping.ProcessingTimeMillisecondsColumnName,
							queuedTask.ProcessingTimeMilliseconds },

						{ mMapping.ErrorCountColumnName,
							queuedTask.ErrorCount },
						{ mMapping.LastErrorColumnName,
							queuedTask.LastError.ToJson() },
						{ mMapping.LastErrorIsRecoverableColumnName,
							queuedTask.LastErrorIsRecoverable },

						{ mMapping.FirstProcessingAttemptedAtTsColumnName,
							queuedTask.FirstProcessingAttemptedAtTs },
						{ mMapping.LastProcessingAttemptedAtTsColumnName,
							queuedTask.LastProcessingAttemptedAtTs },
						{ mMapping.ProcessingFinalizedAtTsColumnName,
							queuedTask.ProcessingFinalizedAtTs }
					};

				await new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.TableName )
					.InsertAsync( insertData, tx );

				mSeededTasks.AddRange( queuedTasks );
			}
		}

		public IEnumerable<QueuedTask> SeededTasks
			=> mSeededTasks.AsReadOnly();
	}
}
