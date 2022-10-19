using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public static class StakhanoviseLoggingProviderExtensions
	{
		public static IStakhanoviseLogger CreateLogger<T>( this IStakhanoviseLoggingProvider loggingProvider )
		{
			if ( loggingProvider == null )
				throw new ArgumentNullException( nameof( loggingProvider ) );

			return loggingProvider.CreateLogger( typeof( T ).FullName );
		}
	}
}
