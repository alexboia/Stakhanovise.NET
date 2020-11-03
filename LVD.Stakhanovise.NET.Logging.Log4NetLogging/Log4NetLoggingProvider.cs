using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Core;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging
{
	public class Log4NetLoggingProvider : IStakhanoviseLoggingProvider
	{
		public IStakhanoviseLogger CreateLogger ( string name )
		{
			ILog log4netLog = LogManager.GetLogger( name );
			return new Log4NetLogger( log4netLog );
		}
	}
}
