using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Ninject;
using LVD.Stakhanovise.NET.Tests.Support;
using LVD.Stakhanovise.NET.Tests.Executors;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class DefaultTaskExecutorRegistryTests
	{
		private IKernel mKernel;

		public DefaultTaskExecutorRegistryTests ()
		{
			mKernel = new StandardKernel( new NinjectTasksTestModule() );
		}

		[Test]
		public void Test_CanScanAssemblies ()
		{
			ITaskExecutorRegistry taskExecutorRegistry =
				CreateTaskExecutorRegistry();

			taskExecutorRegistry.ScanAssemblies( GetType()
				.Assembly );

			Assert.NotNull( taskExecutorRegistry
				.DetectedPayloadTypes );

			Assert.AreEqual( 6, taskExecutorRegistry
				.DetectedPayloadTypes
				.Count() );

			Assert.IsTrue( taskExecutorRegistry.DetectedPayloadTypes
				.Any( p => p.Equals( typeof( AnotherSampleTaskPayload ) ) ) );
			Assert.IsTrue( taskExecutorRegistry.DetectedPayloadTypes
				.Any( p => p.Equals( typeof( SampleTaskPayload ) ) ) );
		}

		[Test]
		public void Test_CanResolveExecutor_PayloadWithExecutor_NoDependencies ()
		{
			ITaskExecutorRegistry taskExecutorRegistry =
				CreateTaskExecutorRegistry();

			taskExecutorRegistry.ScanAssemblies( GetType()
				.Assembly );

			ITaskExecutor nonGenericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor( typeof( SampleTaskPayload ) );

			ITaskExecutor<SampleTaskPayload> genericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor<SampleTaskPayload>();

			Assert.NotNull( nonGenericTaskExecutor );
			Assert.AreEqual( typeof( SampleTaskPayloadExecutor ),
				nonGenericTaskExecutor.GetType() );

			Assert.NotNull( genericTaskExecutor );
			Assert.AreEqual( typeof( SampleTaskPayloadExecutor ),
				genericTaskExecutor.GetType() );
		}

		[Test]
		public void Test_CanResolveExecutor_PayloadWithExecutor_WithDependencies ()
		{
			ITaskExecutorRegistry taskExecutorRegistry =
				CreateTaskExecutorRegistry();

			taskExecutorRegistry.ScanAssemblies( GetType()
				.Assembly );

			ITaskExecutor nonGenericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor( typeof( AnotherSampleTaskPayload ) );

			ITaskExecutor<AnotherSampleTaskPayload> genericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor<AnotherSampleTaskPayload>();

			Assert.NotNull( nonGenericTaskExecutor );
			Assert.AreEqual( typeof( AnotherSampleTaskPayloadExecutor ),
				nonGenericTaskExecutor.GetType() );

			Assert.NotNull( genericTaskExecutor );
			Assert.AreEqual( typeof( AnotherSampleTaskPayloadExecutor ),
				genericTaskExecutor.GetType() );

			AnotherSampleTaskPayloadExecutor asConcreteExecutor =
				genericTaskExecutor as AnotherSampleTaskPayloadExecutor;

			Assert.NotNull( asConcreteExecutor.SampleExecutorDependency );
		}

		[Test]
		public void Test_AttemptResolveExecutor_PayloadWithNoExecutor ()
		{
			ITaskExecutorRegistry taskExecutorRegistry =
				CreateTaskExecutorRegistry();

			taskExecutorRegistry.ScanAssemblies( GetType()
				.Assembly );

			ITaskExecutor nonGenericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor( typeof( SampleNoExecutorPayload ) );

			ITaskExecutor<SampleNoExecutorPayload> genericTaskExecutor = taskExecutorRegistry
				.ResolveExecutor<SampleNoExecutorPayload>();

			Assert.IsNull( nonGenericTaskExecutor );
			Assert.IsNull( genericTaskExecutor );
		}

		private ITaskExecutorRegistry CreateTaskExecutorRegistry ()
		{
			return new DefaultTaskExecutorRegistry( type => mKernel.TryGet( type ) );
		}
	}
}
