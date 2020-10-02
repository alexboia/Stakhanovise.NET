using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueProducerTests : BaseTestWithConfiguration
	{
		private TaskQueueOptions mProducerOptions;

		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskQueueProducerTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mProducerOptions = TestOptions
				.GetDefaultTaskQueueProducerOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mProducerOptions.ConnectionOptions.ConnectionString,
				TestOptions.DefaultMapping,
				queueFaultErrorThrehsoldCount: 5 );
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
		public async Task Test_CanEnqueue ()
		{
			ManualResetEvent notificationWaitHandle =
				new ManualResetEvent( false );

			TaskQueueMetrics previousMetrics,
				newMetrics;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			AbstractTimestamp postedAt = mDataSource.LastPostedAt
				.AddTicks( 1 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer( () => postedAt );

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo( () => postedAt );

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
				notificationWaitHandle.Set();

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer = CreateTaskQueueConsumer( () => postedAt ) )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				previousMetrics = await taskQueueInfo.ComputeMetricsAsync();
				Assert.NotNull( previousMetrics );

				await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: nameof( Test_CanEnqueue ),
					priority: 0 );

				notificationWaitHandle.WaitOne();
				newMetrics = await taskQueueInfo.ComputeMetricsAsync();
				Assert.NotNull( newMetrics );

				//One way to mirror the change is 
				//  to compare the before & after metrics
				Assert.AreEqual( previousMetrics.TotalErrored,
					newMetrics.TotalErrored );
				Assert.AreEqual( previousMetrics.TotalFataled,
					newMetrics.TotalFataled );
				Assert.AreEqual( previousMetrics.TotalFaulted,
					newMetrics.TotalFaulted );
				Assert.AreEqual( previousMetrics.TotalProcessed,
					newMetrics.TotalProcessed );
				Assert.AreEqual( previousMetrics.TotalUnprocessed + 1,
					newMetrics.TotalUnprocessed );

				await taskQueueConsumer.StopReceivingNewTaskUpdatesAsync();
				Assert.IsFalse( taskQueueConsumer.IsReceivingNewTaskUpdates );

				taskQueueConsumer.ClearForDequeue -=
					handleClearForDequeue;
			}
		}

		[Test]
		[Repeat( 5 )]
		public async Task Test_EnqueueDoesNotChangePeekResult_SingleConsumer ()
		{
			int futureTicks = 1;
			IQueuedTask peekTask = null,
				rePeekTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			PostgreSqlTaskQueueProducer taskQueueProducer = CreateTaskQueueProducer( () => mDataSource
				.LastPostedAt
				.AddTicks( 1 ) );

			PostgreSqlTaskQueueInfo taskQueueInfo = CreateTaskQueueInfo( () => mDataSource
				.LastPostedAt
				.AddTicks( futureTicks++ ) );

			peekTask = await taskQueueInfo.PeekAsync();
			Assert.NotNull( peekTask );

			await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					source: nameof( Test_EnqueueDoesNotChangePeekResult_SingleConsumer ),
					priority: 0 );

			rePeekTask = await taskQueueInfo.PeekAsync();
			Assert.NotNull( rePeekTask );

			//Placing a new element in a queue occurs at its end, 
			//  so peeking must not be affected 
			//  if no other operation occurs
			Assert.AreEqual( peekTask.Id,
				rePeekTask.Id );

		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ( Func<AbstractTimestamp> currentTimeProvider )
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions,
				new TestTaskQueueAbstractTimeProvider( currentTimeProvider ) );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
