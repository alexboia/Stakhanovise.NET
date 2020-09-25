using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors
{
	public class DependencyRegistration
	{
		private DependencyRegistration ()
		{
			Scope = DependencyScope.Transient;
		}

		private static void AssertImplementationTypeIsOfTargetType ( Type target, Type asImplementation )
		{
			if ( !target.IsAssignableFrom( asImplementation ) )
				throw new ArgumentException( $"{asImplementation.Name} is not an implementation or sub-class of {target.Name}",
					nameof( asImplementation ) );
		}

		private static void AssertImplementationInstanceIsOfTargetType ( Type target, object asInstance )
		{
			if ( !target.IsAssignableFrom( asInstance.GetType() ) )
				throw new ArgumentException( $"{asInstance.GetType().Name} is not an implementation or sub-class of {target.Name}",
					nameof( asInstance ) );
		}

		private static void AssertProviderIsForTargetType ( Type target, Type asProvider )
		{
			Type checkForProviderType = typeof( IDependencyProvider<> )
				.MakeGenericType( target );

			if ( !checkForProviderType.IsAssignableFrom( asProvider ) )
				throw new ArgumentException( $"{asProvider.Name} is not an implementation of IDependencyProvider<{target}>",
					nameof( asProvider ) );
		}

		public static DependencyRegistration BindToType ( Type target,
			Type asImplementation,
			DependencyScope scope = DependencyScope.Transient )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( asImplementation == null )
				throw new ArgumentNullException( nameof( asImplementation ) );

			AssertImplementationTypeIsOfTargetType( target,
				asImplementation );

			return new DependencyRegistration()
			{
				AsImplementationType = asImplementation,
				Scope = scope,
				Target = target
			};
		}

		public static DependencyRegistration BindToInstance ( Type target,
			object asInstance )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( asInstance == null )
				throw new ArgumentNullException( nameof( asInstance ) );

			AssertImplementationInstanceIsOfTargetType( target,
				asInstance );

			return new DependencyRegistration()
			{
				Target = target,
				Scope = DependencyScope.Transient,
				AsInstance = asInstance
			};
		}

		public static DependencyRegistration BindToProvider ( Type target,
			Type asProvider,
			DependencyScope scope = DependencyScope.Transient )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( asProvider == null )
				throw new ArgumentNullException( nameof( asProvider ) );

			AssertProviderIsForTargetType( target,
				asProvider );

			return new DependencyRegistration()
			{
				Target = target,
				AsProvider = asProvider,
				Scope = scope
			};
		}

		public static DependencyRegistration BindToProviderInstance ( Type target,
			object asProviderInstance,
			DependencyScope scope = DependencyScope.Transient )
		{
			if ( target == null )
				throw new ArgumentNullException( nameof( target ) );

			if ( asProviderInstance == null )
				throw new ArgumentNullException( nameof( asProviderInstance ) );

			return new DependencyRegistration()
			{
				Target = target,
				AsProviderInstance = asProviderInstance,
				Scope = scope
			};
		}

		public Type Target { get; private set; }

		public Type AsImplementationType { get; private set; }

		public Type AsProvider { get; private set; }

		public object AsInstance { get; private set; }

		public object AsProviderInstance { get; private set; }

		public DependencyScope Scope { get; internal set; }
	}
}
