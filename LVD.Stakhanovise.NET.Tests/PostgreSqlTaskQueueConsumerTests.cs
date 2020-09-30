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
	[TestFixture]
	public class PostgreSqlTaskQueueConsumerTests : BaseTestWithConfiguration
	{
		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueConsumerTests ()
		{
			mConsumerOptions = TestOptions.GetDefaultTaskQueueConsumerOptions( ConnectionString );
			mDataSource = new PostgreSqlTaskQueueDataSource( ConnectionString,
				mConsumerOptions.Mapping,
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
		public async Task Test_CanDequeue ()
		{
			List<IQueuedTaskToken> dequedTokens =
				new List<IQueuedTaskToken>();

			IQueuedTaskToken newTaskToken = null,
				previousTaskToken = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue() )
			{
				try
				{
					AbstractTimestamp now =
					new AbstractTimestamp( 2, 2000 );

					for ( int i = 0; i < mConsumerOptions.QueueConsumerConnectionPoolSize; i++ )
					{
						newTaskToken = await taskQueue
							.DequeueAsync( now, taskType );

						Assert.NotNull( newTaskToken );
						Assert.IsTrue( newTaskToken.IsLocked );
						Assert.IsFalse( dequedTokens.Any( t => t.QueuedTask.Id
							== newTaskToken.QueuedTask.Id ) );

						if ( previousTaskToken != null )
							Assert.GreaterOrEqual( newTaskToken.QueuedTask.PostedAtTs,
								previousTaskToken.QueuedTask.PostedAtTs );

						previousTaskToken = newTaskToken;
						dequedTokens.Add( newTaskToken );
					}

					//Double check that the locks are being held
					using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
					{
						foreach ( IQueuedTaskToken t in dequedTokens )
							Assert.IsTrue( await db.IsAdvisoryLockHeldAsync( t.QueuedTask.LockHandleId ) );
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
		[Repeat( 5 )]
		public async Task Test_CanStartStopReceivingNewTaskNotificationUpdates ()
		{
			bool notificationReceived = false;
			ManualResetEvent notificationWaitHandle = new ManualResetEvent( false );

			using ( PostgreSqlTaskQueueConsumer taskQueue = CreateTaskQueue() )
			{
				taskQueue.ClearForDequeue += ( s, e ) =>
				{
					notificationReceived = true;
					notificationWaitHandle.Set();
				};

				await taskQueue.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueue.IsReceivingNewTaskUpdates );

				await SendNewTaskNotificationAsync();
				notificationWaitHandle.WaitOne();
				Assert.IsTrue( notificationReceived );

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


		private async Task<NpgsqlConnection> OpenDbConnectionAsync ()
		{
			NpgsqlConnection db = new NpgsqlConnection( ConnectionString );
			await db.OpenAsync();
			return db;
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueue ()
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions );
		}

		public string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
