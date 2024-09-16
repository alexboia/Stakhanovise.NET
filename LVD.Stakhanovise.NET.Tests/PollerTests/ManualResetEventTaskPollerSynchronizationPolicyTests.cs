using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.PollerTests.Mocks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.PollerTests
{
	[TestFixture]
	public class ManualResetEventTaskPollerSynchronizationPolicyTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanSignalPollerStarted()
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				policy.SignalPollerStarted();
				ClassicAssert.AreEqual( 1, bufferMock.QueuedTaskRetrievedHandlerCount );
				ClassicAssert.AreEqual( 0, bufferMock.QueuedTaskAddedHandlerCount );

				ClassicAssert.AreEqual( 1, consumerMock.ClearForDequeueCount );
			}

			AssertNoMetricsIncremented( metricsProviderMock );
		}

		public Mock<ITaskPollerMetricsProvider> CreateMetricsProviderMock()
		{
			Mock<ITaskPollerMetricsProvider> mock =
				new Mock<ITaskPollerMetricsProvider>();

			mock.Setup( m => m.IncrementPollerWaitForBufferSpaceCount() );
			mock.Setup( m => m.IncrementPollerWaitForDequeueCount() );
			mock.Setup( m => m.IncrementPollerDequeueCount() );
			mock.Setup( m => m.IncrementPollerReturnedTaskCount() );

			return mock;
		}

		private void AssertNoMetricsIncremented( Mock<ITaskPollerMetricsProvider> metricsProviderMock )
		{
			metricsProviderMock.Verify( m => m.IncrementPollerWaitForBufferSpaceCount(),
				Times.Never() );
			metricsProviderMock.Verify( m => m.IncrementPollerWaitForDequeueCount(),
				Times.Never() );
			metricsProviderMock.Verify( m => m.IncrementPollerDequeueCount(),
				Times.Never() );
			metricsProviderMock.Verify( m => m.IncrementPollerReturnedTaskCount(),
				Times.Never() );

			metricsProviderMock.VerifyNoOtherCalls();
		}

		[Test]
		[TestCase( 100 )]
		[TestCase( 250 )]
		[TestCase( 500 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_CanWaitForClearToAddToBuffer_WhenBufferItemRetrieved_WithoutCancellation( int millisecondsDelay )
		{
			RunWaitForClearToAddToBufferTests( millisecondsDelay,
				( buffer, consumer, policy ) => buffer.TriggerQueuedTaskTaskRetrieved() );
		}

		private void RunWaitForClearToAddToBufferTests( int millisecondsDelay,
			Action<MockTaskBufferForSync, MockTaskQueueConsumerForSync, ManualResetEventTaskPollerSynchronizationPolicy> trigger )
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			CancellationTokenSource stopWaitingTokenSource =
				new CancellationTokenSource();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				bufferMock.FillWithMocksToMaxCapacity();
				policy.SignalPollerStarted();

				Task.Delay( millisecondsDelay )
					.ContinueWith( ( prev ) => trigger.Invoke( bufferMock,
						consumerMock,
						policy ) );

				//Purpose of cancellation is to prevent
				//	indefinite waiting for the trigger
				//	and, therefore, to detect erroneous sync policies
				//	and cause tests to fail
				stopWaitingTokenSource
					.CancelAfter( millisecondsDelay * 3 );

				try
				{
					policy.WaitForClearToAddToBuffer( stopWaitingTokenSource.Token );
				}
				catch ( OperationCanceledException )
				{
					Assert.Fail( $"Clear to add to buffer not signaled after {millisecondsDelay * 3} ms." );
				}
			}

			AssertMetricsCallCount( metricsProviderMock,
				waitForBufferSpace: 1,
				waitForDequeue: 0 );
		}

		private void AssertMetricsCallCount( Mock<ITaskPollerMetricsProvider> metricsProviderMock,
			int waitForBufferSpace,
			int waitForDequeue )
		{
			metricsProviderMock.Verify( m => m.IncrementPollerWaitForBufferSpaceCount(),
				Times.Exactly( waitForBufferSpace ) );
			metricsProviderMock.Verify( m => m.IncrementPollerWaitForDequeueCount(),
				Times.Exactly( waitForDequeue ) );
			metricsProviderMock.Verify( m => m.IncrementPollerDequeueCount(),
				Times.Never() );
			metricsProviderMock.Verify( m => m.IncrementPollerReturnedTaskCount(),
				Times.Never() );

			metricsProviderMock.VerifyNoOtherCalls();
		}

		[Test]
		[TestCase( 100 )]
		[TestCase( 250 )]
		[TestCase( 500 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_CanWaitForClearToAddToBuffer_WhenStopSignaled_WithoutCancellation( int millisecondsDelay )
		{
			RunWaitForClearToAddToBufferTests( millisecondsDelay,
				( buffer, consumer, policy ) => policy.SignalPollerStopRequested() );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanWaitForClearToAddToBuffer_WithCancellation()
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			CancellationTokenSource stopWaitingTokenSource =
				new CancellationTokenSource();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				policy.SignalPollerStarted();
				stopWaitingTokenSource.Cancel();

				Assert.Throws<OperationCanceledException>( () => policy
					.WaitForClearToAddToBuffer( stopWaitingTokenSource.Token ) );
			}

			AssertNoMetricsIncremented( metricsProviderMock );
		}

		[Test]
		[TestCase( ClearForDequeReason.NewTaskPostedNotificationReceived, 100 )]
		[TestCase( ClearForDequeReason.NewTaskPostedNotificationReceived, 250 )]
		[TestCase( ClearForDequeReason.NewTaskPostedNotificationReceived, 500 )]
		[TestCase( ClearForDequeReason.NewTaskPostedNotificationReceived, 1000 )]

		[TestCase( ClearForDequeReason.ListenerTimedOut, 100 )]
		[TestCase( ClearForDequeReason.ListenerTimedOut, 250 )]
		[TestCase( ClearForDequeReason.ListenerTimedOut, 500 )]
		[TestCase( ClearForDequeReason.ListenerTimedOut, 1000 )]

		[TestCase( ClearForDequeReason.NewTaskListenerConnectionStateChange, 100 )]
		[TestCase( ClearForDequeReason.NewTaskListenerConnectionStateChange, 250 )]
		[TestCase( ClearForDequeReason.NewTaskListenerConnectionStateChange, 500 )]
		[TestCase( ClearForDequeReason.NewTaskListenerConnectionStateChange, 1000 )]
		[Repeat( 10 )]
		public void Test_WaitForClearToDequeue_WhenReceivedFromConsumer_WithoutCancellation( ClearForDequeReason reason, int millisecondsDelay )
		{
			RunWaitForClearToDequeueTests( millisecondsDelay,
				( buffer, consumer, policy ) => consumer.TriggerClearForDequeue( reason ) );
		}

		[TestCase( 100 )]
		[TestCase( 250 )]
		[TestCase( 500 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_WaitForClearToDequeue_WhenStopSignaled_WithoutCancellation( int millisecondsDelay )
		{
			RunWaitForClearToDequeueTests( millisecondsDelay,
				( buffer, consumer, policy ) => policy.SignalPollerStopRequested() );
		}

		private void RunWaitForClearToDequeueTests( int millisecondsDelay,
			Action<MockTaskBufferForSync, MockTaskQueueConsumerForSync, ManualResetEventTaskPollerSynchronizationPolicy> trigger )
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			CancellationTokenSource stopWaitingTokenSource =
				new CancellationTokenSource();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				policy.SignalPollerStarted();

				Task.Delay( millisecondsDelay )
					.ContinueWith( ( prev ) => trigger.Invoke( bufferMock,
						consumerMock,
						policy ) );

				stopWaitingTokenSource
					.CancelAfter( millisecondsDelay * 2 );

				try
				{
					policy.WaitForClearToDequeue( stopWaitingTokenSource
						.Token );
				}
				catch ( OperationCanceledException )
				{
					Assert.Fail( $"Clear to dequeue not signaled after {millisecondsDelay * 2} ms." );
				}
			}

			AssertMetricsCallCount( metricsProviderMock,
				waitForBufferSpace: 0,
				waitForDequeue: 1 );
		}

		[Test]
		public void Test_WaitForClearToDequeue_WithCancellation()
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			CancellationTokenSource stopWaitingTokenSource =
				new CancellationTokenSource();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				policy.SignalPollerStarted();
				stopWaitingTokenSource.Cancel();

				Assert.Throws<OperationCanceledException>( () => policy
					.WaitForClearToDequeue( stopWaitingTokenSource.Token ) );
			}

			AssertNoMetricsIncremented( metricsProviderMock );
		}

		[Test]
		public void Test_CanSignalPollerStopRequested()
		{
			MockTaskBufferForSync bufferMock =
				new MockTaskBufferForSync();
			MockTaskQueueConsumerForSync consumerMock =
				new MockTaskQueueConsumerForSync();

			Mock<ITaskPollerMetricsProvider> metricsProviderMock =
				CreateMetricsProviderMock();

			using ( ManualResetEventTaskPollerSynchronizationPolicy policy =
				new ManualResetEventTaskPollerSynchronizationPolicy( consumerMock,
				bufferMock,
				metricsProviderMock.Object ) )
			{
				policy.SignalPollerStarted();
				policy.SignalPollerStopRequested();

				ClassicAssert.AreEqual( 0, bufferMock.QueuedTaskRetrievedHandlerCount );
				ClassicAssert.AreEqual( 0, bufferMock.QueuedTaskAddedHandlerCount );
				ClassicAssert.AreEqual( 0, consumerMock.ClearForDequeueCount );
			}

			AssertNoMetricsIncremented( metricsProviderMock );
		}
	}
}
