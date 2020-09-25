using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IDependencySetup
	{
		IDependencySetup BindToType<T, TImplementation> ( DependencyScope scope )
			where TImplementation : T;

		IDependencyBindingScopeSetup BindToType<T, TImplementation> ()
			where TImplementation : T;

		IDependencySetup BindToInstance<T> ( T instance );

		IDependencySetup BindToSelf<T> ( DependencyScope scope );

		IDependencyBindingScopeSetup BindToSelf<T> ();

		IDependencySetup BindToProvider<T, TImplementation, TProvider> ( TImplementation instance,
			DependencyScope scope )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>;

		IDependencyBindingScopeSetup BindToProvider<T, TImplementation, TProvider> ( TImplementation instance )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>;

		IDependencySetup BindToProviderInstance<T, TImplementation> ( IDependencyProvider<TImplementation> implementationProvider,
			DependencyScope scope )
			where TImplementation : T;

		IDependencyBindingScopeSetup BindToProviderInstance<T, TImplementation> ( IDependencyProvider<TImplementation> implementationProvider )
			where TImplementation : T;

		IDependencySetup BindToType ( Type target,
			Type implementationType,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToType ( Type target,
			Type implementationType );

		IDependencySetup BindToInstance ( Type target,
			object instance );

		IDependencySetup BindToSelf ( Type target,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToSelf ( Type target );

		IDependencySetup BindToProvider ( Type target,
			Type providerType,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToProvider ( Type target,
			Type providerType );

		bool HasBindingFor<T> ();

		bool HasBindingFor ( Type target );
	}
}
