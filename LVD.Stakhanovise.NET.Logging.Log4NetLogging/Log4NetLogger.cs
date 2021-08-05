using log4net;
using System;
using System.Collections.Generic;
using System.Text;
using log4net.Core;
using log4net.Util;
using System.Globalization;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging
{
	public class Log4NetLogger : IStakhanoviseLogger
	{
		private ILog mLog4NetLog;

		private Type mThisType;

		public Log4NetLogger( ILog log4NetLog )
		{
			mLog4NetLog = log4NetLog ?? throw new ArgumentNullException( nameof( log4NetLog ) );
			mThisType = GetType();
		}

		public void Debug( string message )
		{
			if ( mLog4NetLog.IsDebugEnabled )
				InternalLog( Level.Debug, message );
		}

		private void InternalLog( Level level, object message )
		{
			mLog4NetLog.Logger.Log( mThisType,
				level,
				message,
				exception: null );
		}

		private void InternalLog( Level level, object message, Exception exception )
		{
			mLog4NetLog.Logger.Log( mThisType,
				level,
				message,
				exception );
		}

		public void DebugFormat( string messageFormat, params object[] args )
		{
			if ( mLog4NetLog.IsDebugEnabled )
				InternalLogFormat( Level.Debug, messageFormat, args );
		}

		private void InternalLogFormat( Level level, string messageFormat, params object[] args )
		{
			InternalLog( level, FormattedLogMessage( messageFormat, args ) );
		}

		private SystemStringFormat FormattedLogMessage( string messageFormat, params object[] args )
		{
			return new SystemStringFormat( CultureInfo.InvariantCulture,
				messageFormat,
				args );
		}

		public void Error( string message )
		{
			if ( mLog4NetLog.IsErrorEnabled )
				InternalLog( Level.Error, message );
		}

		public void Error( string message, Exception exception )
		{
			if ( mLog4NetLog.IsErrorEnabled )
				InternalLog( Level.Error, message, exception );
		}

		public void Fatal( string message )
		{
			if ( mLog4NetLog.IsFatalEnabled )
				InternalLog( Level.Fatal, message );
		}

		public void Fatal( string message, Exception exception )
		{
			if ( mLog4NetLog.IsFatalEnabled )
				InternalLog( Level.Fatal, message, exception );
		}

		public void Info( string message )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				InternalLog( Level.Info, message );
		}

		public void InfoFormat( string messageFormat, params object[] args )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				InternalLogFormat( Level.Info, messageFormat, args );
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
				InternalLog( Level.Info, message );
		}

		public void TraceFormat( string messageFormat, params object[] args )
		{
			if ( mLog4NetLog.IsInfoEnabled )
				InternalLogFormat( Level.Info, messageFormat, args );
		}

		public void Warn( string message )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				InternalLog( Level.Warn, message );
		}

		public void Warn( string message, Exception exception )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				InternalLog( Level.Warn, message, exception );
		}

		public void WarnFormat( string messageFormat, params object[] args )
		{
			if ( mLog4NetLog.IsWarnEnabled )
				InternalLogFormat( Level.Warn, messageFormat, args );
		}
	}
}
