using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Tests.Payloads;
using Moq;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.Worker
{
	[TestFixture]
	public class StandardTaskExecutorResolverTests
	{
		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_CanResolveExecutor( bool withPayload )
		{
			Mock<ITaskExecutorRegistry> registryMock =
				CreateMockRegistry();

			StandardTaskExecutorResolver resolver =
				CreateResolver( registryMock.Object );

			IQueuedTask knownTask =
				CreateKnownQueuedTask( withPayload );

			ITaskExecutor executor = resolver
				.ResolveExecutor( knownTask );

			Assert.NotNull( executor );
			registryMock.Verify();
		}

		private StandardTaskExecutorResolver CreateResolver( ITaskExecutorRegistry registry )
		{
			return new StandardTaskExecutorResolver( registry,
				CreateLogger() );
		}

		private Mock<ITaskExecutorRegistry> CreateMockRegistry()
		{
			Mock<ITaskExecutorRegistry> mock =
				new Mock<ITaskExecutorRegistry>();
			Mock<ITaskExecutor> mockExecuor =
				new Mock<ITaskExecutor>( MockBehavior.Loose );

			Type samplePayloadType =
				typeof( SampleTaskPayload );

			mock.Setup( m => m.ResolvePayloadType( samplePayloadType.FullName ) )
				.Returns( samplePayloadType );

			mock.Setup( m => m.ResolveExecutor( samplePayloadType ) )
				.Returns( mockExecuor.Object );

			mock.Setup( m => m.ResolveExecutor( It.Is<Type>( t => !t.Equals( samplePayloadType ) ) ) )
				.Returns<ITaskExecutor>( null );

			return mock;
		}

		private IQueuedTask CreateKnownQueuedTask( bool withPayload )
		{
			SampleTaskPayload payload =
				new SampleTaskPayload();

			return new QueuedTask()
			{
				Id = Guid.NewGuid(),
				Type = payload.GetType().FullName,
				Payload = withPayload
					? payload
					: null
			};
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_TryResolveExecutor_NoExecutorRegistered( bool withPayload )
		{
			Mock<ITaskExecutorRegistry> registryMock =
				CreateMockRegistry();

			StandardTaskExecutorResolver resolver =
				CreateResolver( registryMock.Object );

			IQueuedTask knownTask =
				CreateUnknownQueuedTask( withPayload );

			ITaskExecutor executor = resolver
				.ResolveExecutor( knownTask );

			Assert.IsNull( executor );
			registryMock.Verify();
		}

		private IQueuedTask CreateUnknownQueuedTask( bool withPayload )
		{
			AnotherSampleTaskPayload payload =
				new AnotherSampleTaskPayload();

			return new QueuedTask()
			{
				Id = Guid.NewGuid(),
				Type = payload.GetType().FullName,
				Payload = withPayload
					? payload
					: null
			};
		}

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}
	}
}
