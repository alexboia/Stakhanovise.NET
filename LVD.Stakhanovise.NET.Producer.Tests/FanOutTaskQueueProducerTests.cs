using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Producer.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class FanOutTaskQueueProducerTests
	{
		private Mock<ITimestampProvider> _timestampProviderMock;

		private Mock<ITaskQueueProducer> _producer1Mock;

		private Mock<ITaskQueueProducer> _producer2Mock;

		private DateTimeOffset _now;

		[SetUp]
		public void Setup()
		{
			_now = DateTimeOffset.UtcNow;
			_timestampProviderMock = new Mock<ITimestampProvider>();
			_timestampProviderMock.Setup(p => p.GetNow()).Returns(_now);

			_producer1Mock = new Mock<ITaskQueueProducer>();
			_producer2Mock = new Mock<ITaskQueueProducer>();
		}

		[Test]
		[TestCase(true, true)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(false, false)]
		public async Task EnqueueAsync_SingleProducer(bool matches, bool stopOnFirstMatch)
		{
			TestPayload payload = new TestPayload();

			// Producer 1 always returns success
			_producer1Mock
				.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				new FanOutTarget( new[] { _producer1Mock.Object }, _ => matches, 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions()
			{
				StopOnFirstMatch = stopOnFirstMatch
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			IQueuedTask result = await producer
				.EnqueueAsync(payload, "test-source", 1);

			ClassicAssert.NotNull(result);

			_producer1Mock.Verify(
				p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
				 matches ? Times.Once : Times.Never
			);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		public async Task EnqueueAsync_ShouldFanOutToAllMatchingProducers(bool stopOnFirstMatch)
		{
			TestPayload payload = new TestPayload();

			// Producer 1 always returns success
			_producer1Mock
				.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			// Producer 2 always returns success
			_producer2Mock
				.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				new FanOutTarget( new[] { _producer1Mock.Object }, _ => true, 1 ),
				new FanOutTarget( new[] { _producer2Mock.Object }, _ => true, 2 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions()
			{
				StopOnFirstMatch = stopOnFirstMatch
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			IQueuedTask result = await producer
				.EnqueueAsync(payload, "test-source", 1);

			ClassicAssert.NotNull(result);

			_producer2Mock.Verify(
				p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
				Times.Once
			);

			if (!stopOnFirstMatch)
			{
				_producer1Mock.Verify(
					p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
					Times.Once
				);
			}
		}

		[Test]
		[TestCase("A", true)]
		[TestCase("A", false)]
		[TestCase("B", true)]
		[TestCase("B", false)]
		[TestCase("C", true)]
		[TestCase("C", false)]
		public async Task EnqueueAsync_ShouldRespectPredicates(string activeCategory, bool stopOnFirstMatch)
		{
			TestPayload payload = new TestPayload
			{
				Category = activeCategory
			};

			_producer1Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			_producer2Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				// Target 1: Only matches Category "A"
				new FanOutTarget( new[] { _producer1Mock.Object },
					info => ((TestPayload)info.Payload).Category == "A", 1 ),

				// Target 2: Only matches Category "B"
				new FanOutTarget( new[] { _producer2Mock.Object },
					info => ((TestPayload)info.Payload).Category == "B", 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions()
			{
				StopOnFirstMatch = stopOnFirstMatch
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			await producer.EnqueueAsync(payload, "test-source", 1);

			switch (activeCategory)
			{
				case "A":
					_producer1Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Once,
						"Producer 1 should have been called"
					);
					_producer2Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Never,
						"Producer 2 should NOT have been called"
					);
					break;

				case "B":
					_producer1Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Never,
						"Producer 1 should NOT have been called"
					);
					_producer2Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Once,
						"Producer 2 should have been called"
					);
					break;

				default:
					_producer1Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Never,
						"Producer 1 should NOT have been called"
					);
					_producer2Mock.Verify(
						p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
						Times.Never,
						"Producer 2 should NOT have been called"
					);
					break;
			}

		}

		[Test]
		public async Task EnqueueAsync_WithCorrelateIds_ShouldUseSameIdForProducers()
		{
			TestPayload payload = new TestPayload();

			Guid? idSeenByProducer1 = null;
			Guid? idSeenByProducer2 = null;

			_producer1Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.Callback<QueuedTaskProduceInfo>(info => idSeenByProducer1 = info.Id)
				.ReturnsAsync((QueuedTaskProduceInfo info) => CreateMockTask(info.Id));

			_producer2Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.Callback<QueuedTaskProduceInfo>(info => idSeenByProducer2 = info.Id)
				.ReturnsAsync((QueuedTaskProduceInfo info) => CreateMockTask(info.Id));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				new FanOutTarget( new[]
				{
					_producer1Mock.Object,
					_producer2Mock.Object
				}, _ => true, 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions
			{
				CorrelateIds = true,
				StopOnFirstMatch = false
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			FanOutQueuedTask result = await producer
				.EnqueueAsync(payload, "test-source", 1)
					as FanOutQueuedTask;

			ClassicAssert.IsNotNull(idSeenByProducer1);
			ClassicAssert.IsNotNull(idSeenByProducer2);
			ClassicAssert.AreNotEqual(Guid.Empty, idSeenByProducer1);
			ClassicAssert.AreEqual(
				idSeenByProducer1,
				idSeenByProducer2,
				"Both producers should have received the same Task ID"
			);

			ClassicAssert.IsNotNull(result);
			ClassicAssert.AreEqual(2, result.AllTasks.Count());
			ClassicAssert.AreEqual(0, result.Errors.Count());

			foreach (IQueuedTask t in result.AllTasks)
				ClassicAssert.AreEqual(idSeenByProducer1, t.Id);
		}

		[Test]
		public async Task EnqueueAsync_WithStopOnFirstMatch_ShouldOnlyCallHighestPriorityMatch()
		{
			TestPayload payload = new TestPayload();

			/* Setup both mocks to return successful tasks */
			_producer1Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));
			_producer2Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				// High Priority
				new FanOutTarget( new[] { _producer1Mock.Object }, _ => true, 100 ),

				// Low Priority
				new FanOutTarget( new[] { _producer2Mock.Object }, _ => true, 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions
			{
				StopOnFirstMatch = true
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			await producer.EnqueueAsync(payload, "test-source", 1);

			_producer1Mock.Verify(
				p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
				Times.Once
				);
			_producer2Mock.Verify(
				p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()),
				Times.Never
				);
		}

		[Test]
		public void EnqueueAsync_WithErrorPolicyThrowOnAny_ShouldThrowAggregateException()
		{
			TestPayload payload = new TestPayload();

			_producer1Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			_producer2Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ThrowsAsync(new Exception("Producer 2 Failed"));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				new FanOutTarget( new[]
				{
					_producer1Mock.Object,
					_producer2Mock.Object
				}, _ => true, 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions
			{
				ErrorPolicy = FanOutErrorPolicy.ThrowOnAnyError
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			AggregateException ex = Assert.ThrowsAsync<AggregateException>(async () =>
				await producer.EnqueueAsync(payload, "test-source", 1));

			Assert.That(ex.InnerExceptions.Count,
				Is.EqualTo(1));
			Assert.That(ex.InnerExceptions.First().Message,
				Is.EqualTo("Producer 2 Failed"));
		}

		[Test]
		public async Task EnqueueAsync_WithErrorPolicySucceedIfAny_ShouldReturnTaskWithExceptions()
		{
			// Arrange
			TestPayload payload = new TestPayload();

			// One succeeds
			_producer1Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ReturnsAsync(CreateMockTask(Guid.NewGuid()));

			// One fails
			_producer2Mock.Setup(p => p.EnqueueAsync(It.IsAny<QueuedTaskProduceInfo>()))
				.ThrowsAsync(new InvalidOperationException("Producer 2 failure"));

			List<FanOutTarget> targets = new List<FanOutTarget>
			{
				new FanOutTarget( new[]
				{
					_producer1Mock.Object,
					_producer2Mock.Object
				}, _ => true, 1 )
			};

			FanOutTaskQueueProducerOptions options = new FanOutTaskQueueProducerOptions
			{
				ErrorPolicy = FanOutErrorPolicy.SucceedIfAny
			};

			FanOutTaskQueueProducer producer = new FanOutTaskQueueProducer(targets,
				_timestampProviderMock.Object,
				options);

			// Act
			IQueuedTask result = await producer.EnqueueAsync(payload, "test-source", 1);

			// Assert
			ClassicAssert.IsNotNull(result);
			ClassicAssert.IsInstanceOf<FanOutQueuedTask>(result);

			FanOutQueuedTask fanOutResult = (FanOutQueuedTask)result;
			ClassicAssert.AreEqual(1, fanOutResult.Errors.Count());
			ClassicAssert.IsInstanceOf<InvalidOperationException>(fanOutResult.Errors.First());
			ClassicAssert.AreEqual(1, fanOutResult.AllTasks.Count());
		}

		private IQueuedTask CreateMockTask(Guid id)
		{
			var taskMock = new Mock<IQueuedTask>();
			taskMock.SetupGet(t => t.Id).Returns(id);
			return taskMock.Object;
		}

		private class TestPayload
		{
			public string Category
			{
				get; set;
			}
		}
	}
}