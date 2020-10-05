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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Executors;
using NuGet.Frameworks;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StandardDependencyResolverTests
	{
		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public void Test_CanLoad_AndResolve_BindToType ( int nTestThreads )
		{
			StandardDependencyResolver resolver =
				new StandardDependencyResolver();

			List<DependencyRegistration> dependencies =
				new List<DependencyRegistration>();

			dependencies.Add( DependencyRegistration.BindToType( typeof( IAsSingletonSampleDependency ),
				typeof( AsSingletonSampleDependencyImpl ),
				DependencyScope.Singleton ) );

			dependencies.Add( DependencyRegistration.BindToType( typeof( IAsThreadSingletonSampleDependency ),
				typeof( AsThreadSingletonSampleDependencyImpl ),
				DependencyScope.Thread ) );

			dependencies.Add( DependencyRegistration.BindToType( typeof( IAsTransientSampleDependency ),
				typeof( AsTransientSampleDependencyImpl ),
				DependencyScope.Transient ) );

			resolver.Load( dependencies );

			Assert_DependenciesCanBeResolved( resolver );
			Assert_DependenciesCorrectlyResolved( nTestThreads, resolver );
		}

		[Test]
		public void Test_CanLoad_AndResolve_BindToInstance ()
		{
			StandardDependencyResolver resolver =
				new StandardDependencyResolver();

			List<DependencyRegistration> dependencies =
				new List<DependencyRegistration>();

			IAsSingletonSampleDependency instance = new AsSingletonSampleDependencyImpl();

			dependencies.Add( DependencyRegistration.BindToInstance( typeof( IAsSingletonSampleDependency ),
				instance ) );

			resolver.Load( dependencies );

			Assert.IsTrue( resolver.CanResolve( typeof( IAsSingletonSampleDependency ) ) );
			Assert.IsTrue( resolver.CanResolve<IAsSingletonSampleDependency>() );

			Assert.AreSame( instance,
				resolver.TryResolve<IAsSingletonSampleDependency>() );

			Assert.AreSame( resolver.TryResolve<IAsSingletonSampleDependency>(),
				resolver.TryResolve<IAsSingletonSampleDependency>() );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public void Test_CanLoad_AndResolve_BindToProviderType ( int nTestThreads )
		{
			StandardDependencyResolver resolver =
				new StandardDependencyResolver();

			List<DependencyRegistration> dependencies =
				new List<DependencyRegistration>();

			dependencies.Add( DependencyRegistration.BindToProvider( typeof( IAsSingletonSampleDependency ),
				typeof( AsSingletonSampleDependencyProvider ),
				DependencyScope.Singleton ) );

			dependencies.Add( DependencyRegistration.BindToProvider( typeof( IAsThreadSingletonSampleDependency ),
				typeof( AsThreadSingletonSampleDependencyProvider ),
				DependencyScope.Thread ) );

			dependencies.Add( DependencyRegistration.BindToProvider( typeof( IAsTransientSampleDependency ),
				typeof( AsTransientSampleDependencyProvider ),
				DependencyScope.Transient ) );

			resolver.Load( dependencies );

			Assert_DependenciesCanBeResolved( resolver );
			Assert_DependenciesCorrectlyResolved( nTestThreads, resolver );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public void Test_CanLoad_AndResolve_BindToProviderInstance ( int nTestThreads )
		{
			StandardDependencyResolver resolver =
				new StandardDependencyResolver();

			List<DependencyRegistration> dependencies =
				new List<DependencyRegistration>();

			dependencies.Add( DependencyRegistration.BindToProviderInstance( typeof( IAsSingletonSampleDependency ),
				new AsSingletonSampleDependencyProvider(),
				DependencyScope.Singleton ) );

			dependencies.Add( DependencyRegistration.BindToProviderInstance( typeof( IAsThreadSingletonSampleDependency ),
				new AsThreadSingletonSampleDependencyProvider(),
				DependencyScope.Thread ) );

			dependencies.Add( DependencyRegistration.BindToProviderInstance( typeof( IAsTransientSampleDependency ),
				new AsTransientSampleDependencyProvider(),
				DependencyScope.Transient ) );

			resolver.Load( dependencies );

			Assert_DependenciesCanBeResolved( resolver );
			Assert_DependenciesCorrectlyResolved( nTestThreads, resolver );
		}

		private static void Assert_DependenciesCorrectlyResolved ( int nTestThreads, StandardDependencyResolver resolver )
		{
			//Check that singletons are resolved as such
			Assert.AreSame( resolver.TryResolve<IAsSingletonSampleDependency>(),
				resolver.TryResolve<IAsSingletonSampleDependency>() );

			//Check that thread singletons are resolved as such
			List<Task> threads =
				new List<Task>();

			ConcurrentDictionary<int, IAsThreadSingletonSampleDependency> threadInstances =
				new ConcurrentDictionary<int, IAsThreadSingletonSampleDependency>();

			for ( int i = 0; i < nTestThreads; i++ )
			{
				threads.Add( Task.Run( () =>
				{
					IAsThreadSingletonSampleDependency dep = resolver
					   .TryResolve<IAsThreadSingletonSampleDependency>();

					Assert.AreSame( dep, resolver
					   .TryResolve<IAsThreadSingletonSampleDependency>() );

					threadInstances.AddOrUpdate( Thread.CurrentThread.ManagedThreadId,
						( key ) => dep,
						( key, oldDep ) => dep );
				} ) );
			}

			Task.WaitAll( threads.ToArray() );

			IAsThreadSingletonSampleDependency[] checkThreadInstances = threadInstances
				.Values
				.ToArray();

			for ( int i = 1; i < checkThreadInstances.Length; i++ )
			{
				Assert.AreNotSame( checkThreadInstances[ i - 1 ],
					checkThreadInstances[ i ] );
			}

			//Check that transients are resolved as such
			Assert.AreNotSame( resolver.TryResolve<IAsTransientSampleDependency>(),
				resolver.TryResolve<IAsTransientSampleDependency>() );
		}

		private static void Assert_DependenciesCanBeResolved ( StandardDependencyResolver resolver )
		{
			Assert.IsTrue( resolver.CanResolve( typeof( IAsSingletonSampleDependency ) ) );
			Assert.IsTrue( resolver.CanResolve( typeof( IAsThreadSingletonSampleDependency ) ) );
			Assert.IsTrue( resolver.CanResolve( typeof( IAsTransientSampleDependency ) ) );
			Assert.IsFalse( resolver.CanResolve( typeof( IDependencyNotRegisteredWithResolver ) ) );

			Assert.IsTrue( resolver.CanResolve<IAsSingletonSampleDependency>() );
			Assert.IsTrue( resolver.CanResolve<IAsThreadSingletonSampleDependency>() );
			Assert.IsTrue( resolver.CanResolve<IAsTransientSampleDependency>() );
			Assert.IsFalse( resolver.CanResolve<IDependencyNotRegisteredWithResolver>() );

			Assert.IsInstanceOf<AsSingletonSampleDependencyImpl>( resolver.TryResolve( typeof( IAsSingletonSampleDependency ) ) );
			Assert.IsInstanceOf<AsSingletonSampleDependencyImpl>( resolver.TryResolve<IAsSingletonSampleDependency>() );

			Assert.IsInstanceOf<AsThreadSingletonSampleDependencyImpl>( resolver.TryResolve( typeof( IAsThreadSingletonSampleDependency ) ) );
			Assert.IsInstanceOf<AsThreadSingletonSampleDependencyImpl>( resolver.TryResolve<IAsThreadSingletonSampleDependency>() );

			Assert.IsInstanceOf<AsTransientSampleDependencyImpl>( resolver.TryResolve( typeof( IAsTransientSampleDependency ) ) );
			Assert.IsInstanceOf<AsTransientSampleDependencyImpl>( resolver.TryResolve<IAsTransientSampleDependency>() );

			Assert.IsNull( resolver.TryResolve( typeof( IDependencyNotRegisteredWithResolver ) ) );
			Assert.IsNull( resolver.TryResolve<IDependencyNotRegisteredWithResolver>() );
		}
	}
}
