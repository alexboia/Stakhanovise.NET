using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public static class StakhanoviseLogManager
	{
		private static bool mProviderRetrieved;

		private static IStakhanoviseLoggingProvider mProvider = new NoOpLoggingProvider();

		internal static IStakhanoviseLogger GetLogger ( Type type )
		{
			return GetLogger( type.FullName );
		}

		internal static IStakhanoviseLogger GetLogger ( string name )
		{
			return Provider.CreateLogger( "Stakhanovise." + name );
		}

		public static IStakhanoviseLoggingProvider Provider
		{
			get
			{
				mProviderRetrieved = true;
				return mProvider;
			}
			set
			{
				if ( mProviderRetrieved )
					throw new InvalidOperationException( "The logging provider must be set before any Npgsql action is taken" );

				mProvider = value ?? throw new ArgumentNullException( nameof( value ) );
			}
		}
	}
}
