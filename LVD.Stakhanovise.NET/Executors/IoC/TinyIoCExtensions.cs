using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors.IoC
{
	public static class TinyIoCExtensions
	{
		private static object CreateoObjectWithProvider ( Type targetType,
			IDependencyProvider provider,
			IDependencyResolver resolver )
		{
			if ( !targetType.IsAssignableFrom( provider.Type ) )
				throw new InvalidOperationException( $"{provider.GetType()} does not produce {targetType} instances" );

			return provider.CreateInstance( resolver );
		}

		public static TinyIoCContainer.RegisterOptions RegisterWithProviderType ( this TinyIoCContainer container,
			Type targetType,
			Type asProviderType,
			IDependencyResolver resolver )
		{
			if ( container == null )
				throw new ArgumentNullException( nameof( container ) );

			if ( targetType == null )
				throw new ArgumentNullException( nameof( targetType ) );

			if ( asProviderType == null )
				throw new ArgumentNullException( nameof( asProviderType ) );

			if ( resolver == null )
				throw new ArgumentNullException( nameof( resolver ) );

			return container.Register( targetType, ( c, p ) =>
			{
				IDependencyProvider provider = Activator.CreateInstance( asProviderType )
					as IDependencyProvider;

				if ( provider == null )
					throw new InvalidOperationException( $"{targetType} is not a valid provider instance" );

				return CreateoObjectWithProvider( targetType,
					provider,
					resolver );
			} );
		}

		public static TinyIoCContainer.RegisterOptions RegisterWithProviderInstance ( this TinyIoCContainer container,
			Type targetType,
			IDependencyProvider provider,
			IDependencyResolver resolver )
		{
			if ( container == null )
				throw new ArgumentNullException( nameof( container ) );

			if ( targetType == null )
				throw new ArgumentNullException( nameof( targetType ) );

			if ( provider == null )
				throw new ArgumentNullException( nameof( provider ) );

			if ( resolver == null )
				throw new ArgumentNullException( nameof( resolver ) );

			return container.Register( targetType, ( c, p ) =>
			{
				return CreateoObjectWithProvider( targetType,
					provider,
					resolver
					);
			} );
		}
	}
}
