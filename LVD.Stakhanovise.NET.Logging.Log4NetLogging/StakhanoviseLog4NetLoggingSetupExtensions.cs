using LVD.Stakhanovise.NET.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging
{
	public static class StakhanoviseLog4NetLoggingSetupExtensions
	{
		public static IStakhanoviseSetup WithLog4NetLogging ( this IStakhanoviseSetup setup )
		{
			if ( setup == null )
				throw new ArgumentNullException( nameof( setup ) );

			return setup.WithLoggingProvider( new Log4NetLoggingProvider() );
		}
	}
}
