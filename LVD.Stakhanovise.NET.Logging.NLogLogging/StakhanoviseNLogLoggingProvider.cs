using System;
using System.Collections.Generic;
using System.Text;
using NLog;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging
{
	public class StakhanoviseNLogLoggingProvider : IStakhanoviseLoggingProvider
	{
		public IStakhanoviseLogger CreateLogger ( string name )
		{
			Logger nlogLog = LogManager.GetLogger( name );
			return new StakhanoviseNLogLogger( nlogLog );
		}
	}
}
