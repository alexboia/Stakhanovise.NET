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
	public class PostgreSqlTaskProducerTests : BaseTestWithConfiguration
	{
		private TaskQueueOptions mProducerOptions;

		private TaskQueueInfoOptions mInfoOptions;

		private TaskQueueConsumerOptions mConsumerOptions;

		private PostgreSqlTaskQueueDataSource mDataSource;

		public PostgreSqlTaskProducerTests ()
		{
			mInfoOptions = TestOptions
				.GetDefaultTaskQueueInfoOptions( ConnectionString );
			mProducerOptions = TestOptions
				.GetDefaultTaskQueueProducerOptions( ConnectionString );
			mConsumerOptions = TestOptions
				.GetDefaultTaskQueueConsumerOptions( ConnectionString );

			mDataSource = new PostgreSqlTaskQueueDataSource( mProducerOptions.ConnectionOptions.ConnectionString,
				mProducerOptions.Mapping,
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
			bool notificationReceived = false;
			ManualResetEvent notificationWaitHandle = new ManualResetEvent( false );

			TaskQueueMetrics previousMetrics,
				newMetrics;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			AbstractTimestamp postedAt =
				new AbstractTimestamp( 1, 1000 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer();

			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo();

			EventHandler<ClearForDequeueEventArgs> handleClearForDequeue = ( s, e ) =>
			{
				notificationReceived = true;
				notificationWaitHandle.Set();
			};

			using ( PostgreSqlTaskQueueConsumer taskQueueConsumer =
				CreateTaskQueueConsumer() )
			{
				taskQueueConsumer.ClearForDequeue +=
					handleClearForDequeue;

				await taskQueueConsumer.StartReceivingNewTaskUpdatesAsync();
				Assert.IsTrue( taskQueueConsumer.IsReceivingNewTaskUpdates );

				previousMetrics = await taskQueueInfo.ComputeMetricsAsync();
				Assert.NotNull( previousMetrics );

				await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					now: postedAt,
					source: nameof( Test_CanEnqueue ),
					priority: 0 );

				notificationWaitHandle.WaitOne();
				Assert.IsTrue( notificationReceived );

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
			IQueuedTask peekTask = null,
				rePeekTask = null;

			string taskType = typeof( SampleTaskPayload )
				.FullName;

			AbstractTimestamp initialPeekAt =
				new AbstractTimestamp( 2, 1000 );

			AbstractTimestamp postedAt =
				new AbstractTimestamp( 2, 1000 );

			AbstractTimestamp peekAt =
				new AbstractTimestamp( 3, 2000 );

			PostgreSqlTaskQueueProducer taskQueueProducer =
				CreateTaskQueueProducer();
			PostgreSqlTaskQueueInfo taskQueueInfo =
				CreateTaskQueueInfo();

			peekTask = await taskQueueInfo.PeekAsync( initialPeekAt );
			Assert.NotNull( peekTask );

			await taskQueueProducer.EnqueueAsync( payload: new SampleTaskPayload( 100 ),
					now: postedAt,
					source: nameof( Test_EnqueueDoesNotChangePeekResult_SingleConsumer ),
					priority: 0 );

			rePeekTask = await taskQueueInfo.PeekAsync( peekAt );
			Assert.NotNull( rePeekTask );

			//Placing a new element in a queue occurs at its end, 
			//  so peeking must not be affected 
			//  if no other operation occurs
			Assert.AreEqual( peekTask.Id,
				rePeekTask.Id );

		}

		private PostgreSqlTaskQueueProducer CreateTaskQueueProducer ()
		{
			return new PostgreSqlTaskQueueProducer( mProducerOptions );
		}

		private PostgreSqlTaskQueueInfo CreateTaskQueueInfo ()
		{
			return new PostgreSqlTaskQueueInfo( mInfoOptions );
		}

		private PostgreSqlTaskQueueConsumer CreateTaskQueueConsumer ()
		{
			return new PostgreSqlTaskQueueConsumer( mConsumerOptions );
		}

		private string ConnectionString
			=> GetConnectionString( "baseTestDbConnectionString" );
	}
}
