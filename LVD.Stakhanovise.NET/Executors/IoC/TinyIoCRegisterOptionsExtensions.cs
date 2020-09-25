using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors.IoC
{
	public static class TinyIoCRegisterOptionsExtensions
	{
		public static TinyIoCContainer.RegisterOptions WithScopeFromRegistrationInfo ( this TinyIoCContainer.RegisterOptions regOpts, 
			DependencyRegistration registration )
		{
			if ( regOpts == null )
				throw new ArgumentNullException( nameof( regOpts ) );

			if ( registration == null )
				throw new ArgumentNullException( nameof( registration ) );
			
			switch ( registration.Scope )
			{
				case DependencyScope.Singleton:
					regOpts.AsSingleton();
					break;
				case DependencyScope.Thread:
					regOpts.AsPerRequestSingleton();
					break;
				case DependencyScope.Transient:
					regOpts.AsMultiInstance();
					break;
			}

			return regOpts;
		}
	}
}
