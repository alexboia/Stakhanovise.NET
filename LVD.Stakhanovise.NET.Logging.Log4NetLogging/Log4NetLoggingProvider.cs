using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Core;
using System.IO;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging
{
	public class Log4NetLoggingProvider : IStakhanoviseLoggingProvider
	{
		public IStakhanoviseLogger CreateLogger( string name )
		{
			Configure();
			ILog log4netLog = LogManager.GetLogger( name );
			return new Log4NetLogger( log4netLog );
		}

		private void Configure()
		{
			FileInfo fileInfo = new FileInfo( GetLog4NetXmlFilePath() );
			log4net.Config.XmlConfigurator.Configure( fileInfo );
		}

		private string GetLog4NetXmlFilePath()
		{
			return Path.Combine( Directory.GetCurrentDirectory(), "log4net.config" );
		}
	}
}
