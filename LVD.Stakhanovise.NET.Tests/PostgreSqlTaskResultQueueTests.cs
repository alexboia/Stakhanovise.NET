using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using Bogus;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskResultQueueTests : BaseDbTests
	{
		private PostgreSqlTaskQueueDataSource mDataSource;

		private TaskQueueOptions mResultQueueOptions;

		public PostgreSqlTaskResultQueueTests ()
		{
			mResultQueueOptions = TestOptions
				.GetDefaultTaskResultQueueOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mResultQueueOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				queueFaultErrorThrehsoldCount: 5 );
		}

		[Test]
		[TestCase( 1, 0 )]
		[TestCase( 1, 100 )]
		[TestCase( 1, 1000 )]
		[TestCase( 1, 10000 )]

		[TestCase( 2, 0 )]
		[TestCase( 2, 100 )]
		[TestCase( 2, 1000 )]
		[TestCase( 2, 10000 )]

		[TestCase( 5, 0 )]
		[TestCase( 5, 100 )]
		[TestCase( 5, 1000 )]

		[TestCase( 10, 0 )]
		[TestCase( 10, 100 )]
		[TestCase( 10, 1000 )]
		public async Task Test_CanStartStop ( int repeatCycles, int timeBetweenStartStopCalls )
		{
			using ( PostgreSqlTaskResultQueue rq = CreateResultQueue() )
			{
				for ( int i = 0; i < repeatCycles; i++ )
				{
					await rq.StartAsync();
					Assert.IsTrue( rq.IsRunning );

					if ( timeBetweenStartStopCalls > 0 )
						await Task.Delay( timeBetweenStartStopCalls );

					await rq.StopAsync();
					Assert.IsFalse( rq.IsRunning );
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanPostResult_SuccessfulResults_SerialCalls ()
		{
			Faker faker = new Faker();
			Func<TaskExecutionResult> rsFactory = () => new TaskExecutionResult( TaskExecutionResultInfo.Successful(),
				duration: faker.Date.Timespan(),
				retryAtTicks: 0,
				faultErrorThresholdCount: mDataSource.QueueFaultErrorThresholdCount );

			await Run_PostResultTests( rsFactory );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanPostResult_FailedResults_SerialCalls ()
		{
			Faker faker = new Faker();
			Func<TaskExecutionResult> rsFactory = () => new TaskExecutionResult( TaskExecutionResultInfo
					.ExecutedWithError( new QueuedTaskError( faker.System.Exception() ),
						isRecoverable: faker.Random.Bool() ),
				duration: TimeSpan.Zero,
				retryAtTicks: faker.Random.Long( 100 ),
				faultErrorThresholdCount: mDataSource.QueueFaultErrorThresholdCount );

			await Run_PostResultTests( rsFactory );
		}

		private async Task Run_PostResultTests ( Func<TaskExecutionResult> rsFactory )
		{
			using ( PostgreSqlTaskResultQueue rq = CreateResultQueue() )
			{
				await rq.StartAsync();

				foreach ( IQueuedTaskToken token in mDataSource.SeededTaskTokens )
				{
					token.UdpateFromExecutionResult( rsFactory.Invoke() );
					int affectedRows = await rq.PostResultAsync( token );

					Assert.AreEqual( 1, affectedRows );

					QueuedTaskResult dbResult = await mDataSource
						.GetQueuedTaskResultFromDbByIdAsync( token.DequeuedTask.Id );

					dbResult.AssertMatchesResult( token
						.LastQueuedTaskResult );
				}

				await rq.StopAsync();
			}
		}

		private PostgreSqlTaskResultQueue CreateResultQueue ()
		{
			return new PostgreSqlTaskResultQueue( mResultQueueOptions );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
