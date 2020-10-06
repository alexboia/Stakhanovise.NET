using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using NUnit.Framework;
using System.Threading;
using LVD.Stakhanovise.NET.Tests.Helpers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlQueuedTaskTokenTests : BaseDbTests
	{
		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlQueuedTaskTokenTests ()
		{
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mConsumerOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				mConsumerOptions.FaultErrorThresholdCount );
		}

		[SetUp]
		public async Task TestSetUp ()
		{
			await mDataSource.SeedData();
			await Task.Delay( 100 );
		}

		[TearDown]
		public async Task TestTearDown ()
		{
			await mDataSource.ClearData();
			await Task.Delay( 100 );
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_ReleaseBeforeSettingStarted ()
		{
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => mDataSource.LastPostedAt ) )
			using ( IQueuedTaskToken token = await taskQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );
				using ( PostgreSqlQueuedTaskTokenMonitor monitor = new PostgreSqlQueuedTaskTokenMonitor( ( PostgreSqlQueuedTaskToken )token ) )
				{
					await token.ReleaseLockAsync();

					Assert.IsFalse( token.IsLocked );
					Assert.IsFalse( token.IsPending );
					Assert.IsFalse( token.IsActive );
					Assert.IsTrue( monitor.TokenReleasedEventCalled );

					Assert.IsFalse( await token.TrySetStartedAsync( 100 ) );
					Assert.IsFalse( await token.TrySetResultAsync( SuccessfulExecutionResult() ) );
					Assert.IsFalse( await token.TrySetResultAsync( ErrorExecutionResult( now.Ticks + 100 ) ) );

					//Double check that the locks are NOT being held
					await AssertTokenLockStatus( token, expectedStatus: false );
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetSetarted ()
		{
			long futureTicks = 1;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			List<IQueuedTaskToken> dequeuedByCompetingQueue =
				new List<IQueuedTaskToken>();

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( PostgreSqlTaskQueueConsumer competingQueue = CreateTaskQueue( () => now.AddTicks( futureTicks++ ) ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				Assert.IsTrue( await token.TrySetStartedAsync( now.TickDuration * 10 ) );

				Assert.IsTrue( token.IsActive );
				Assert.IsTrue( token.IsLocked );
				Assert.IsFalse( token.IsPending );

				//Check db task status
				IQueuedTask dbTask = await mDataSource.GetQueuedTaskByIdAsync( token.DequeuedTask.Id );

				Assert.NotNull( dbTask );
				Assert.AreEqual( QueuedTaskStatus.Processing, dbTask.Status );
				Assert.AreEqual( token.DequeuedTask.LockedUntil, dbTask.LockedUntil );

				//Double check that the locks are being held
				await AssertTokenLockStatus( token, expectedStatus: true );

				//Attempt some competing dequeues
				long timeDelta = token.DequeuedTask.LockedUntil
					- token.DequeuedAt.Ticks;

				for ( long iCompete = 0; iCompete < timeDelta; iCompete++ )
				{
					using ( IQueuedTaskToken newToken = await competingQueue.DequeueAsync() )
					{
						Assert.AreNotEqual( token.DequeuedTask.Id, newToken.DequeuedTask.Id );
						await newToken.ReleaseLockAsync();
					}
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetResult_ExecutedSuccessfully ()
		{
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );
				using ( PostgreSqlQueuedTaskTokenMonitor monitor = new PostgreSqlQueuedTaskTokenMonitor( ( PostgreSqlQueuedTaskToken )token ) )
				{
					Assert.IsTrue( await token.TrySetStartedAsync( now.TickDuration * 10 ) );

					//Simulate working on task
					await Task.Delay( 1000 );

					Assert.IsTrue( await token.TrySetResultAsync( SuccessfulExecutionResult() ) );

					Assert.IsFalse( token.IsActive );
					Assert.IsFalse( token.IsLocked );
					Assert.IsFalse( token.IsPending );
					Assert.IsTrue( monitor.TokenReleasedEventCalled );

					//Check db task status
					IQueuedTask dbTask = await mDataSource.GetQueuedTaskByIdAsync( token.DequeuedTask.Id );

					Assert.NotNull( dbTask );
					Assert.AreEqual( QueuedTaskStatus.Processed, dbTask.Status );

					//Double check that the locks are NOT being held anymore
					await AssertTokenLockStatus( token, expectedStatus: false );
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetResult_ExecutedWithError ()
		{
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );
				using ( PostgreSqlQueuedTaskTokenMonitor monitor = new PostgreSqlQueuedTaskTokenMonitor( ( PostgreSqlQueuedTaskToken )token ) )
				{
					Assert.IsTrue( await token.TrySetStartedAsync( now.TickDuration * 10 ) );

					//Simulate working on task
					await Task.Delay( 1000 );

					int expectedErrorCount = token.DequeuedTask.ErrorCount + 1;
					QueuedTaskStatus expectedStatus = QueuedTaskStatus.Error;

					if ( expectedErrorCount > mConsumerOptions.FaultErrorThresholdCount + 1 )
						expectedStatus = QueuedTaskStatus.Fatal;
					else if ( expectedErrorCount >= mConsumerOptions.FaultErrorThresholdCount )
						expectedStatus = QueuedTaskStatus.Faulted;

					Assert.IsTrue( await token.TrySetResultAsync( ErrorExecutionResult( now.Ticks + 100 ) ) );

					Assert.IsFalse( token.IsActive );
					Assert.IsFalse( token.IsLocked );
					Assert.IsFalse( token.IsPending );
					Assert.IsTrue( monitor.TokenReleasedEventCalled );

					//Check db task status
					IQueuedTask dbTask = await mDataSource.GetQueuedTaskByIdAsync( token.DequeuedTask.Id );

					Assert.NotNull( dbTask );
					Assert.AreEqual( expectedStatus, dbTask.Status );
					Assert.AreEqual( expectedErrorCount, dbTask.ErrorCount );
					Assert.AreEqual( now.Ticks + 100, dbTask.LockedUntil );

					//Double check that the locks are NOT being held anymore
					await AssertTokenLockStatus( token, expectedStatus: false );
				}
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		[Repeat( 3 )]
		public async Task Test_TokenLifeCycle_CanHandleConnectionDropouts ( int nDropouts )
		{
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				PostgreSqlQueuedTaskToken postgresToken =
					( PostgreSqlQueuedTaskToken )token;

				using ( PostgreSqlQueuedTaskTokenMonitor monitor = new PostgreSqlQueuedTaskTokenMonitor( postgresToken ) )
				{
					Assert.IsTrue( await postgresToken.TrySetStartedAsync( now.TickDuration * 10 ) );

					for ( int iDropout = 0; iDropout < nDropouts; iDropout++ )
					{
						monitor.Reset();

						await WaitAndTerminateConnectionAsync( postgresToken.ConnectionBackendProcessId,
							syncHandle: null,
							timeout: 100 );

						monitor.WaitForTokenConnectionDroppedInvocation();
						monitor.WaitForConnectionEstablishedInvocation();

						Assert.IsFalse( monitor.TokenReleasedEventCalled );
						Assert.IsFalse( monitor.CancellationTokenInvoked );

						Assert.GreaterOrEqual( postgresToken.ConnectionBackendProcessId, 0 );

						Assert.IsTrue( token.IsActive );
						Assert.IsTrue( token.IsLocked );
						Assert.IsFalse( token.IsPending );

						//Double check that the locks are being held
						await AssertTokenLockStatus( token, expectedStatus: true );
					}
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CorrectTokenHandover_CompetingDequeueWhileConnectionIsDown ()
		{
			IQueuedTaskToken newToken = null;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( PostgreSqlTaskQueueConsumer competingQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				PostgreSqlQueuedTaskToken postgresToken =
						( PostgreSqlQueuedTaskToken )token;

				using ( PostgreSqlQueuedTaskTokenMonitor monitor = new PostgreSqlQueuedTaskTokenMonitor( postgresToken ) )
				{
					monitor.SetUserCallbackForConnectionStateChange( PostgreSqlQueuedTaskTokenConnectionState.AttemptingToReconnect, () =>
					{
						now = now.FromTicks( postgresToken.DequeuedTask.LockedUntil + 1 );
						newToken = competingQueue.Dequeue();
					} );

					Assert.IsTrue( await postgresToken.TrySetStartedAsync( now.TickDuration * 10 ) );

					await WaitAndTerminateConnectionAsync( postgresToken.ConnectionBackendProcessId,
						syncHandle: null,
						timeout: 100 );

					monitor.WaitForTokenConnectionDroppedInvocation();
					monitor.WaitForConnectionFailedInvocation();

					//Check that the new dequeue request claimed the token 
					//	whose connection failed
					Assert.AreEqual( postgresToken.DequeuedTask.Id,
						newToken.DequeuedTask.Id );

					//Old token:
					//	- check token has been released;
					//	- check associated cancellation token has been invoked
					//	- check its connection has been dismantled
					//	- check not locked, not active and not pending
					Assert.IsTrue( monitor.TokenReleasedEventCalled );
					Assert.IsTrue( monitor.CancellationTokenInvoked );
					Assert.AreEqual( postgresToken.ConnectionBackendProcessId, -1 );

					Assert.IsFalse( postgresToken.IsActive );
					Assert.IsFalse( postgresToken.IsLocked );
					Assert.IsFalse( postgresToken.IsPending );

					//New token:
					//	- must be pending, locked and not yet active
					Assert.NotNull( newToken );
					Assert.IsTrue( newToken.IsPending );
					Assert.IsTrue( newToken.IsLocked );
					Assert.IsFalse( newToken.IsActive );

					//Double check that the locks are being held
					await AssertTokenLockStatus( token, expectedStatus: true );

					await newToken.ReleaseLockAsync();
					newToken.Dispose();
				}
			}
		}

		private void AssertTokenIsValidPostgresToken ( IQueuedTaskToken token )
		{
			Assert.NotNull( token );
			Assert.IsInstanceOf<PostgreSqlQueuedTaskToken>( token );
		}

		private async Task AssertTokenLockStatus ( IQueuedTaskToken token, bool expectedStatus )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
				Assert.AreEqual( expectedStatus, await db.IsAdvisoryLockHeldAsync( token.DequeuedTask.LockHandleId ) );
		}

		private TaskExecutionResult SuccessfulExecutionResult ()
		{
			TaskExecutionResultInfo resultInfo = TaskExecutionResultInfo
				.Successful();

			return new TaskExecutionResult( new TimedExecutionResult<TaskExecutionResultInfo>( resultInfo,
				TimeSpan.FromMilliseconds( 100 ) ) );
		}

		private TaskExecutionResult ErrorExecutionResult ( long retryAtTicks = 0 )
		{
			TaskExecutionResultInfo resultInfo = TaskExecutionResultInfo
				.ExecutedWithError( new QueuedTaskError( new Exception( "Sample exception" ) ),
					isRecoverable: false );

			return new TaskExecutionResult( new TimedExecutionResult<TaskExecutionResultInfo>( resultInfo,
				TimeSpan.FromMilliseconds( 0 ) ), retryAtTicks );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueue ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			return await OpenDbConnectionAsync( ConnectionString );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
