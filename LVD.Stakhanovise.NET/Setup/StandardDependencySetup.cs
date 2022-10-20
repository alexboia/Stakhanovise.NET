// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardDependencySetup : IDependencySetup
	{
		private List<IDependencyRegistration> mDependencyRegistrations =
			new List<IDependencyRegistration>();

		public IDependencySetup BindToInstance<T>( T instance )
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToInstance( typeof( T ),
				asInstance: instance ) );

			return this;
		}

		public IDependencySetup BindToInstance( Type target, object instance )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( instance == null )
				throw new ArgumentNullException( nameof( instance ) );

			if ( HasBindingFor( target ) )
				throw new InvalidOperationException( $"Target type {target.Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToInstance( target,
				asInstance: instance ) );

			return this;
		}

		public IDependencySetup BindToProvider<T, TImplementation, TProvider>( TImplementation instance,
			DependencyScope scope )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToProvider( typeof( T ),
				asProvider: typeof( TProvider ),
				scope: scope ) );

			return this;
		}

		public IDependencyBindingScopeSetup BindToProvider<T, TImplementation, TProvider>( TImplementation instance )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			DependencyRegistration reg = DependencyRegistration.BindToProvider( typeof( T ),
				asProvider: typeof( TProvider ),
				scope: DependencyScope.Transient );

			mDependencyRegistrations.Add( reg );
			return new StandardDependencyBindingScopeSetup( reg );
		}

		public IDependencySetup BindToProviderInstance<T, TImplementation>( IDependencyProvider<TImplementation> implementationProvider,
			DependencyScope scope )
			where TImplementation : T
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToProviderInstance( typeof( T ),
				asProviderInstance: implementationProvider,
				scope: scope ) );

			return this;
		}

		public IDependencyBindingScopeSetup BindToProviderInstance<T, TImplementation>( IDependencyProvider<TImplementation> implementationProvider )
			where TImplementation : T
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			DependencyRegistration reg = DependencyRegistration.BindToProviderInstance( typeof( T ),
				asProviderInstance: implementationProvider,
				scope: DependencyScope.Transient );

			mDependencyRegistrations.Add( reg );
			return new StandardDependencyBindingScopeSetup( reg );
		}

		public IDependencySetup BindToProvider( Type target,
			Type providerType,
			DependencyScope scope )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( providerType == null )
				throw new ArgumentNullException( nameof( providerType ) );

			if ( HasBindingFor( target ) )
				throw new InvalidOperationException( $"Target type {target.Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToProvider( target,
				asProvider: providerType,
				scope: scope ) );

			return this;
		}

		public IDependencyBindingScopeSetup BindToProvider( Type target,
			Type providerType )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( providerType == null )
				throw new ArgumentNullException( nameof( providerType ) );

			if ( HasBindingFor( target ) )
				throw new InvalidOperationException( $"Target type {target.Name} is already bound." );

			DependencyRegistration reg = DependencyRegistration.BindToProvider( target,
				asProvider: providerType,
				scope: DependencyScope.Transient );

			mDependencyRegistrations.Add( reg );
			return new StandardDependencyBindingScopeSetup( reg );
		}

		public IDependencySetup BindToType<T, TImplementation>( DependencyScope scope )
			where TImplementation : T
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToType( typeof( T ),
				asImplementation: typeof( TImplementation ),
				scope: scope ) );

			return this;
		}

		public IDependencyBindingScopeSetup BindToType<T, TImplementation>()
			where TImplementation : T
		{
			if ( HasBindingFor<T>() )
				throw new InvalidOperationException( $"Target type {typeof( T ).Name} is already bound." );

			DependencyRegistration reg = DependencyRegistration.BindToType( typeof( T ),
				asImplementation: typeof( TImplementation ),
				scope: DependencyScope.Transient );

			mDependencyRegistrations.Add( reg );
			return new StandardDependencyBindingScopeSetup( reg );
		}

		public IDependencySetup BindToType( Type target,
			Type implementationType,
			DependencyScope scope )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( implementationType == null )
				throw new ArgumentNullException( nameof( implementationType ) );

			if ( HasBindingFor( target ) )
				throw new InvalidOperationException( $"Target type {target.Name} is already bound." );

			mDependencyRegistrations.Add( DependencyRegistration.BindToType( target,
				asImplementation: implementationType,
				scope: scope ) );

			return this;
		}

		public IDependencyBindingScopeSetup BindToType( Type target,
			Type implementationType )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( implementationType == null )
				throw new ArgumentNullException( nameof( implementationType ) );

			if ( HasBindingFor( target ) )
				throw new InvalidOperationException( $"Target type {target.Name} is already bound." );

			DependencyRegistration reg = DependencyRegistration.BindToType( target,
				asImplementation: implementationType,
				scope: DependencyScope.Transient );

			mDependencyRegistrations.Add( reg );
			return new StandardDependencyBindingScopeSetup( reg );
		}

		public IDependencySetup BindToSelf<T>( DependencyScope scope )
		{
			return BindToType<T, T>( scope );
		}

		public IDependencyBindingScopeSetup BindToSelf<T>()
		{
			return BindToType<T, T>();
		}

		public IDependencySetup BindToSelf( Type target, DependencyScope scope )
		{
			return BindToType( target,
				target,
				scope );
		}

		public IDependencyBindingScopeSetup BindToSelf( Type target )
		{
			return BindToType( target, target );
		}

		public bool HasBindingFor<T>()
		{
			return HasBindingFor( typeof( T ) );
		}

		public bool HasBindingFor( Type target )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			return mDependencyRegistrations.Any( r => r.Target.Equals( target ) );
		}

		public IEnumerable<IDependencyRegistration> DependencyRegistrations
			=> mDependencyRegistrations;
	}
}
