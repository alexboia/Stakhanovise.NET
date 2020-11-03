using LVD.Stakhanovise.NET.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging
{
	public static class StakhanoviseNLogLoggingSetupExtensions
	{
		public static IStakhanoviseSetup WithNLogLogging ( this IStakhanoviseSetup setup )
		{
			if ( setup == null )
				throw new ArgumentNullException( nameof( setup ) );

			return setup.WithLoggingProvider( new NLogLoggingProvider() );
		}
	}
}
