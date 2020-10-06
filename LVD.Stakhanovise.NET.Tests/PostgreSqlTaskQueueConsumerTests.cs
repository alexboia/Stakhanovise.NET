using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: also test dequeue with tasks still being locked
	//TODO: test correct handling of connection dropouts (notifications being emitted by queue consumer)
	//TODO: also test without a given task type/s
	//TODO: test for all test task types/payload types
	[TestFixture]
	public class PostgreSqlTaskQueueConsumerTests : BaseDbTests
	{
		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueConsumerTests ()
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
		public async Task Test_CanDequeue_WithTaskTypes ()
		{
			List<IQueuedTaskToken> dequedTokens =
				new List<IQueuedTaskToken>();

			IQueuedTaskToken newTaskToken = null,
				previousTaskToken = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			AbstractTimestamp now = mDataSource
				.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => now ) )
			{
				try
				{
					for ( int i = 0; i < mConsumerOptions.QueueConsumerConnectionPoolSize; i++ )
					{
						newTaskToken = await taskQueue
							.DequeueAsync( taskType );

						Assert.NotNull( newTaskToken );
						Assert.NotNull( newTaskToken.DequeuedAt );
						Assert.NotNull( newTaskToken.DequeuedTask );

						Assert.AreEqual( now, newTaskToken.DequeuedAt );

						Assert.IsTrue( newTaskToken.IsLocked );
						Assert.IsTrue( newTaskToken.IsPending );

						Assert.IsFalse( dequedTokens.Any( t => t.DequeuedTask.Id
							== newTaskToken.DequeuedTask.Id ) );

						Assert.IsTrue( newTaskToken.DequeuedTask.Status == QueuedTaskStatus.Unprocessed
							|| newTaskToken.DequeuedTask.Status == QueuedTaskStatus.Error
							|| newTaskToken.DequeuedTask.Status == QueuedTaskStatus.Faulted );

						if ( previousTaskToken != null )
							Assert.GreaterOrEqual( newTaskToken.DequeuedTask.PostedAt,
								previousTaskToken.DequeuedTask.PostedAt );

						previousTaskToken = newTaskToken;
						dequedTokens.Add( newTaskToken );
					}

					//Double check that the locks are being held
					using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
					{
						foreach ( IQueuedTaskToken t in dequedTokens )
							Assert.IsTrue( await db.IsAdvisoryLockHeldAsync( t.DequeuedTask.LockHandleId ) );
					}
				}
				finally
				{
					foreach ( IQueuedTaskToken t in dequedTokens )
					{
						await t.ReleaseLockAsync();
						t.Dispose();
					}
				}
			}
		}

		[Test]
		public async Task Test_Dequeue_ThenRelease_ProducesSameToken ()
		{
			IQueuedTaskToken token;
			Guid firstTokenId = Guid.Empty;

			AbstractTimestamp now = mDataSource
				.LastPostedAt;

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => now ) )
			{
				using ( token = await taskQueue.DequeueAsync() )
				{
					if ( !firstTokenId.Equals( Guid.Empty ) )
						Assert.AreEqual( firstTokenId, token.DequeuedTask.Id );
					else
						firstTokenId = token.DequeuedTask.Id;

					await token.ReleaseLockAsync();
				}
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_CanStartStopReceivingNewTaskNotificationUpdates ()
		{
			ManualResetEvent notificationWaitHandle = new
				ManualResetEvent( false );

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue( () => mDataSource.LastPostedAt ) )
			{
				taskQueue.ClearForDequeue += ( s, e ) =>
					notificationWaitHandle.Set();

				await taskQueue.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await SendNewTaskNotificationAsync();
				notificationWaitHandle.WaitOne();

				await taskQueue.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueue.IsReceivingNewTaskUpdates );
			}
		}

		private async Task SendNewTaskNotificationAsync ()
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
				await db.NotifyAsync( mConsumerOptions.Mapping.NewTaskNotificaionChannelName, null );
			await Task.Delay( 100 );
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
