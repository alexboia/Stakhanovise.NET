// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Executors;
using LVD.Stakhanovise.NET.Tests.Payloads;
using LVD.Stakhanovise.NET.Tests.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Tests
{
	//TODO: add tests for loading dependencies directly into the task executor registry
	[TestFixture]
	public class StandardTaskExecutorRegistryTests
	{
		private const int ExpectedTestPayloadCount = 12;
		
		[Test]
		public void Test_CanScanAssemblies ()
		{
			ITaskExecutorRegistry taskExecutorRegistry =
				CreateTaskExecutorRegistry();

			taskExecutorRegistry.ScanAssemblies( GetType()
				.Assembly );

			Assert.NotNull( taskExecutorRegistry
				.DetectedPayloadTypes );

			Assert.AreEqual( ExpectedTestPayloadCount, taskExecutorRegistry
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
			return new StandardTaskExecutorRegistry( GetDependencyResolver() );
		}

		private IDependencyResolver GetDependencyResolver ()
		{
			IDependencyResolver dependencyResolver =
				new StandardDependencyResolver();

			dependencyResolver.Load( TestDependencyRegistrations
				.GetAll() );

			return dependencyResolver;
		}
	}
}
