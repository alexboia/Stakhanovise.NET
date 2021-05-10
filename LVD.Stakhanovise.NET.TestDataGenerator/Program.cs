using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Bogus;
using Npgsql;
using System.Threading.Tasks;
using SqlKata.Execution;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Helpers;
using SqlKata.Compilers;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.TestDataGenerator
{
	public class Program
	{
		private IConfiguration mConfiguration;

		private QueuedTaskMapping mMapping;

		public Program ()
		{
			mMapping = new QueuedTaskMapping();
			mConfiguration = GetConfig();
		}

		public static void Main ( string[] args )
		{
			new Program().Run( args );
		}

		public void Run ( string[] args )
		{
			Console.WriteLine( "Generating test data..." );
			ClearQueuedTasksTestData();
			CreateQueuedTasksTestData( 1000000 );
			Console.WriteLine( "Test data generated. Press any key to close this window." );
			Console.ReadKey();
		}

		private void CreateQueuedTasksTestData ( int count )
		{
			List<QueuedTask> tasksData = GenerateQueuedTasksData( count );
			AddQueuedTasks( tasksData );
		}

		private List<QueuedTask> GenerateQueuedTasksData ( int count )
		{
			List<QueuedTask> tasksData = new List<QueuedTask>( count );
			Faker<QueuedTask> queuedTaskFaker = CreateQueuedTaskFaker();

			for ( int i = 0; i < count; i++ )
				tasksData.Add( queuedTaskFaker.Generate() );

			return tasksData;
		}

		private Faker<QueuedTask> CreateQueuedTaskFaker ()
		{
			string baseNamespace = GetType().Namespace;
			string[] payloadTypes = new string[]
			{
				 baseNamespace + ".TypeA",
				 baseNamespace + ".TypeB",
				 baseNamespace + ".TypeC",
				 baseNamespace + ".TypeD",
				 baseNamespace + ".TypeE",
				 baseNamespace + ".TypeF",
				 baseNamespace + ".TypeG",
				 baseNamespace + ".TypeH"
			};

			return new Faker<QueuedTask>()
				.RuleFor( q => q.Id, ( f, q ) => Guid.NewGuid() )
				.RuleFor( q => q.LockedUntilTs, ( f, q ) => f.Random.Int() % 3 == 0 ? f.Date.PastOffset() : f.Date.FutureOffset() )
				.RuleFor( q => q.Payload, ( f, q ) => "{}" )
				.RuleFor( q => q.PostedAtTs, ( f, q ) => f.Date.RecentOffset() )
				.RuleFor( q => q.Priority, ( f, q ) => f.Random.Int( 0, 10 ) )
				.RuleFor( q => q.Source, ( f, q ) => nameof( CreateQueuedTaskFaker ) )
				.RuleFor( q => q.Type, ( f, q ) => f.PickRandom( payloadTypes ) );
		}

		private void AddQueuedTasks ( List<QueuedTask> tasksData )
		{
			using ( NpgsqlConnection conn = OpenDbConnection() )
			using ( NpgsqlTransaction tx = conn.BeginTransaction() )
			{
				foreach ( QueuedTask t in tasksData )
					AddQueuedTask( t, conn, tx );

				tx.Commit();
			}
		}

		private void AddQueuedTask ( QueuedTask taskData,
			NpgsqlConnection conn,
			NpgsqlTransaction tx )
		{
			Dictionary<string, object> insertDataTask = new Dictionary<string, object>()
			{
				{ "task_id", taskData.Id },
				{ "task_payload", taskData.Payload.ToJson(includeTypeInformation: true) },
				{ "task_type", taskData.Type },

				{ "task_source", taskData.Source },
				{ "task_priority", taskData.Priority },

				{ "task_posted_at_ts", taskData.PostedAtTs },
				{ "task_locked_until_ts", taskData.LockedUntilTs }
			};

			if ( tx != null )
				new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Insert( insertDataTask, tx );
			else
				new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Insert( insertDataTask );
		}

		private void ClearQueuedTasksTestData ()
		{
			using ( NpgsqlConnection conn = OpenDbConnection() )
			{
				new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.QueueTableName )
					.Delete();
			}
		}

		private void ClearQueuedTaskResultsTestData ()
		{
			using ( NpgsqlConnection conn = OpenDbConnection() )
			{
				new QueryFactory( conn, new PostgresCompiler() )
					.Query( mMapping.ResultsQueueTableName )
					.Delete();
			}
		}

		private NpgsqlConnection OpenDbConnection ()
		{
			NpgsqlConnection db = new NpgsqlConnection( GetTestDbConnectionString() );
			db.Open();
			return db;
		}

		private string GetTestDbConnectionString ()
		{
			return GetConnectionString( "testDbConnectionString" );
		}

		private string GetConnectionString ( string connectionStringName )
		{
			if ( string.IsNullOrEmpty( connectionStringName ) )
				throw new ArgumentNullException( nameof( connectionStringName ) );

			return mConfiguration.GetConnectionString( connectionStringName );
		}

		private IConfiguration GetConfig ()
		{
			return new ConfigurationBuilder()
				.SetBasePath( Directory.GetCurrentDirectory() )
				.AddJsonFile( "appsettings.json", false, true )
				.Build();
		}
	}
}
