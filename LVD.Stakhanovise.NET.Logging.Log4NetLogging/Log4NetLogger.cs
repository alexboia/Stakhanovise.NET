using log4net;
using System;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging
{
	public class Log4NetLogger : IStakhanoviseLogger
	{
		private ILog mLog4NetLog;

		public Log4NetLogger( ILog log4NetLog )
		{
			mLog4NetLog = log4NetLog 
				?? throw new ArgumentNullException( nameof( log4NetLog ) );
		}

		public void Debug( string message )
		{
			if ( mLog4NetLog.IsDebugEnabled )
				mLog4NetLog.Debug( message );
		}

		public void DebugFormat( string messageFormat, params object [] args )
		{
			if ( mLog4NetLog.IsDebugEnabled )
				mLog4NetLog.DebugFormat(messageFormat, args);
		}

		public void Error( string message )
		{
			if ( mLog4NetLog.IsErrorEnabled )
				mLog4NetLog.Error( message );
		}

		public void Error( string message, Exception exception )
		{
			if ( mLog4NetLog.IsErrorEnabled )
				mLog4NetLog.Error( message, exception );
		}

		public void Fatal( string message )
		{
			if ( mLog4NetLog.IsFatalEnabled )
				mLog4NetLog.Fatal( message );
		}

		public void Fatal( string message, Exception exception )
		{
			if ( mLog4NetLog.IsFatalEnabled )
				mLog4NetLog.Fatal( message, exception );
		}

		public void Info( string message )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				mLog4NetLog.Info( message );
		}

		public void InfoFormat( string messageFormat, params object [] args )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				mLog4NetLog.InfoFormat( messageFormat, args );
		}

		public bool IsEnabled( StakhanoviseLogLevel level )
		{
			switch ( level )
			{
				case StakhanoviseLogLevel.Debug:
					return mLog4NetLog.IsDebugEnabled;
				case StakhanoviseLogLevel.Error:
					return mLog4NetLog.IsErrorEnabled;
				case StakhanoviseLogLevel.Fatal:
					return mLog4NetLog.IsFatalEnabled;
				case StakhanoviseLogLevel.Info:
					return mLog4NetLog.IsInfoEnabled;
				case StakhanoviseLogLevel.Trace:
					return mLog4NetLog.IsInfoEnabled;
				case StakhanoviseLogLevel.Warn:
					return mLog4NetLog.IsWarnEnabled;
				default:
					return false;
			}
		}

		public void Trace( string message )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				mLog4NetLog.Info( message );
		}

		public void TraceFormat( string messageFormat, params object [] args )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				mLog4NetLog.InfoFormat( messageFormat, args );
		}

		public void Warn( string message )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				mLog4NetLog.Warn( message );
		}

		public void Warn( string message, Exception exception )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				mLog4NetLog.Warn( message, exception );
		}

		public void WarnFormat( string messageFormat, params object [] args )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				mLog4NetLog.WarnFormat( messageFormat, args );
		}
	}
}
