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
			bool tokenReleasedEventCalled = false;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => mDataSource.LastPostedAt ) )
			using ( IQueuedTaskToken token = await taskQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				token.TokenReleased += ( s, e )
					=> tokenReleasedEventCalled = true;

				await token.ReleaseLockAsync();

				Assert.IsFalse( token.IsLocked );
				Assert.IsFalse( token.IsPending );
				Assert.IsFalse( token.IsActive );
				Assert.IsTrue( tokenReleasedEventCalled );

				Assert.IsFalse( await token.TrySetStartedAsync( 100 ) );
				Assert.IsFalse( await token.TrySetResultAsync( SuccessfulExecutionResult() ) );
				Assert.IsFalse( await token.TrySetResultAsync( ErrorExecutionResult( now.Ticks + 100 ) ) );

				//Double check that the locks are NOT being held
				await AssertTokenLockStatus( token, expectedStatus: false );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetSetarted ()
		{
			long futureTicks = 0;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now.AddTicks( futureTicks++ ) ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				Assert.IsTrue( await token
					.TrySetStartedAsync( now.TickDuration * 10 ) );

				Assert.IsTrue( token.IsActive );
				Assert.IsTrue( token.IsLocked );
				Assert.IsFalse( token.IsPending );

				//Check db task status
				IQueuedTask dbTask = await mDataSource
					.GetQueuedTaskByIdAsync( token.QueuedTask.Id );

				Assert.NotNull( dbTask );
				Assert.AreEqual( QueuedTaskStatus.Processing,
					dbTask.Status );
				Assert.AreEqual( token.QueuedTask.LockedUntil,
					dbTask.LockedUntil );

				//Double check that the locks are being held
				await AssertTokenLockStatus( token, expectedStatus: true );

				//Attempt some competing dequeues
				long timeDelta = token.QueuedTask.LockedUntil
					- token.DequeuedAt.Ticks;

				for ( long iCompete = 0; iCompete < timeDelta; iCompete++ )
				{
					using ( IQueuedTaskToken newToken = await consumerQueue.DequeueAsync() )
					{
						Assert.NotNull( newToken );
						Assert.AreNotEqual( token.QueuedTask.Id, newToken.QueuedTask.Id );
						await newToken.ReleaseLockAsync();
					}
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetResult_ExecutedSuccessfully ()
		{
			bool tokenReleasedEventCalled = false;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				token.TokenReleased += ( s, e )
					=> tokenReleasedEventCalled = true;

				Assert.IsTrue( await token
					.TrySetStartedAsync( now.TickDuration * 10 ) );

				//Simulate working on task
				await Task.Delay( 1000 );

				Assert.IsTrue( await token.TrySetResultAsync( SuccessfulExecutionResult() ) );

				Assert.IsFalse( token.IsActive );
				Assert.IsFalse( token.IsLocked );
				Assert.IsFalse( token.IsPending );
				Assert.IsTrue( tokenReleasedEventCalled );

				//Check db task status
				IQueuedTask dbTask = await mDataSource
					.GetQueuedTaskByIdAsync( token.QueuedTask.Id );

				Assert.NotNull( dbTask );
				Assert.AreEqual( QueuedTaskStatus.Processed,
					dbTask.Status );

				//Double check that the locks are NOT being held anymore
				await AssertTokenLockStatus( token, expectedStatus: false );
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CanSetResult_ExecutedWithError ()
		{
			bool tokenReleasedEventCalled = false;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
			{
				AssertTokenIsValidPostgresToken( token );

				token.TokenReleased += ( s, e )
					=> tokenReleasedEventCalled = true;

				Assert.IsTrue( await token
					.TrySetStartedAsync( now.TickDuration * 10 ) );

				//Simulate working on task
				await Task.Delay( 1000 );

				int expectedErrorCount = token.QueuedTask.ErrorCount + 1;
				QueuedTaskStatus expectedStatus = QueuedTaskStatus.Error;

				if ( expectedErrorCount > mConsumerOptions.FaultErrorThresholdCount + 1 )
					expectedStatus = QueuedTaskStatus.Fatal;
				else if ( expectedErrorCount >= mConsumerOptions.FaultErrorThresholdCount )
					expectedStatus = QueuedTaskStatus.Faulted;

				Assert.IsTrue( await token.TrySetResultAsync( ErrorExecutionResult( now.Ticks + 100 ) ) );

				Assert.IsFalse( token.IsActive );
				Assert.IsFalse( token.IsLocked );
				Assert.IsFalse( token.IsPending );
				Assert.IsTrue( tokenReleasedEventCalled );

				//Check db task status
				IQueuedTask dbTask = await mDataSource
					.GetQueuedTaskByIdAsync( token.QueuedTask.Id );

				Assert.NotNull( dbTask );
				Assert.AreEqual( expectedStatus, dbTask.Status );
				Assert.AreEqual( expectedErrorCount, dbTask.ErrorCount );
				Assert.AreEqual( now.Ticks + 100, dbTask.LockedUntil );

				//Double check that the locks are NOT being held anymore
				await AssertTokenLockStatus( token, expectedStatus: false );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		[Repeat( 3 )]
		public async Task Test_TokenLifeCycle_CanHandleConnectionDropouts ( int nDropouts )
		{
			bool tokenReleasedEventCalled = false;
			bool cancellationTokenInvoked = false;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			{
				using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
				using ( ManualResetEvent tokenConnectionDroppedHandle = new ManualResetEvent( false ) )
				using ( ManualResetEvent tokenConnectionEstablishedHandle = new ManualResetEvent( false ) )
				{
					AssertTokenIsValidPostgresToken( token );

					PostgreSqlQueuedTaskToken postgresToken =
						( PostgreSqlQueuedTaskToken )token;

					postgresToken.TokenReleased += ( s, e )
						=> tokenReleasedEventCalled = true;

					postgresToken.ConnectionStateChanged += ( s, e ) =>
					{
						if ( e.NewState == PostgreSqlQueuedTaskTokenConnectionState.Dropped )
							tokenConnectionDroppedHandle.Set();
						else if ( e.NewState == PostgreSqlQueuedTaskTokenConnectionState.Established )
							tokenConnectionEstablishedHandle.Set();
					};

					postgresToken.CancellationToken.Register( ()
						=> cancellationTokenInvoked = true );

					Assert.IsTrue( await postgresToken
						.TrySetStartedAsync( now.TickDuration * 10 ) );

					for ( int iDropout = 0; iDropout < nDropouts; iDropout++ )
					{
						tokenReleasedEventCalled = false;
						cancellationTokenInvoked = false;

						await WaitAndTerminateConnectionAsync( postgresToken.ConnectionBackendProcessId,
							syncHandle: null,
							timeout: 100 );

						tokenConnectionDroppedHandle.WaitOne();
						tokenConnectionEstablishedHandle.WaitOne();

						Assert.IsFalse( tokenReleasedEventCalled );
						Assert.IsFalse( cancellationTokenInvoked );

						Assert.GreaterOrEqual( postgresToken.ConnectionBackendProcessId, 0 );

						Assert.IsTrue( token.IsActive );
						Assert.IsTrue( token.IsLocked );
						Assert.IsFalse( token.IsPending );

						//Double check that the locks are being held
						await AssertTokenLockStatus( token, expectedStatus: true );

						tokenConnectionDroppedHandle.Reset();
						tokenConnectionEstablishedHandle.Reset();
					}
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_TokenLifeCycle_CorrectTokenHandover_CompetingDequeueWhileConnectionIsDown ()
		{
			IQueuedTaskToken newToken = null;
			bool tokenReleasedEventCalled = false;
			bool cancellationTokenInvoked = false;
			AbstractTimestamp now = mDataSource.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer consumerQueue = CreateTaskQueue( () => now ) )
			using ( PostgreSqlTaskQueueConsumer competingQueue = CreateTaskQueue( () => now ) )
			{
				using ( IQueuedTaskToken token = await consumerQueue.DequeueAsync() )
				using ( ManualResetEvent tokenConnectionDroppedHandle = new ManualResetEvent( false ) )
				using ( ManualResetEvent tokenConnectionEstablishedOrFailedHandle = new ManualResetEvent( false ) )
				{
					AssertTokenIsValidPostgresToken( token );

					PostgreSqlQueuedTaskToken postgresToken =
						( PostgreSqlQueuedTaskToken )token;

					postgresToken.TokenReleased += ( s, e )
						=> tokenReleasedEventCalled = true;

					postgresToken.ConnectionStateChanged += ( s, e ) =>
					{
						if ( e.NewState == PostgreSqlQueuedTaskTokenConnectionState.AttemptingToReconnect )
						{
							//Manipulate timeline
							now = now.FromTicks( postgresToken.QueuedTask.LockedUntil + 1 );
							newToken = competingQueue.Dequeue();
						}
						else if ( e.NewState == PostgreSqlQueuedTaskTokenConnectionState.Dropped )
							tokenConnectionDroppedHandle.Set();
						else
							tokenConnectionEstablishedOrFailedHandle.Set();
					};

					postgresToken.CancellationToken.Register( ()
						=> cancellationTokenInvoked = true );

					Assert.IsTrue( await postgresToken
						.TrySetStartedAsync( now.TickDuration * 10 ) );

					await WaitAndTerminateConnectionAsync( postgresToken.ConnectionBackendProcessId,
						syncHandle: null,
						timeout: 100 );

					tokenConnectionDroppedHandle.WaitOne();
					tokenConnectionEstablishedOrFailedHandle.WaitOne();

					Assert.AreEqual( postgresToken.QueuedTask.Id,
						newToken.QueuedTask.Id );

					Assert.IsTrue( tokenReleasedEventCalled );
					Assert.IsTrue( cancellationTokenInvoked );
					Assert.AreEqual( postgresToken.ConnectionBackendProcessId, -1 );

					Assert.IsFalse( token.IsActive );
					Assert.IsFalse( token.IsLocked );
					Assert.IsFalse( token.IsPending );

					Assert.NotNull( newToken );
					Assert.IsTrue( newToken.IsPending );
					Assert.IsTrue( newToken.IsLocked );
					Assert.IsFalse( newToken.IsActive );

					//Double check that the locks are being held
					await AssertTokenLockStatus( token, expectedStatus: true );

					await newToken.ReleaseLockAsync();
					newToken.Dispose();

					tokenConnectionDroppedHandle.Reset();
					tokenConnectionEstablishedOrFailedHandle.Reset();
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
				Assert.AreEqual( expectedStatus, await db.IsAdvisoryLockHeldAsync( token.QueuedTask.LockHandleId ) );
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
