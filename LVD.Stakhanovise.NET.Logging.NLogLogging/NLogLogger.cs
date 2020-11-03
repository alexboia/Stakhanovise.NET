using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging
{
	public class NLogLogger : IStakhanoviseLogger
	{
		private Logger mNlogLog;

		public NLogLogger ( Logger nlogLog )
		{
			mNlogLog = nlogLog ?? throw new ArgumentNullException( nameof( nlogLog ) );
		}

		public void Debug ( string message )
		{
			mNlogLog.Debug( message );
		}

		public void DebugFormat ( string messageFormat, params object[] args )
		{
			mNlogLog.Debug( messageFormat, args );
		}

		public void Error ( string message )
		{
			mNlogLog.Error( message );
		}

		public void Error ( string message, Exception exception )
		{
			mNlogLog.Error( exception, message );
		}

		public void Fatal ( string message )
		{
			mNlogLog.Fatal( message );
		}

		public void Fatal ( string message, Exception exception )
		{
			mNlogLog.Fatal( exception, message );
		}

		public void Info ( string message )
		{
			mNlogLog.Info( message );
		}

		public void InfoFormat ( string messageFormat, params object[] args )
		{
			mNlogLog.Info( messageFormat, args );
		}

		public bool IsEnabled ( StakhanoviseLogLevel level )
		{
			switch ( level )
			{
				case StakhanoviseLogLevel.Debug:
					return mNlogLog.IsDebugEnabled;
				case StakhanoviseLogLevel.Error:
					return mNlogLog.IsErrorEnabled;
				case StakhanoviseLogLevel.Fatal:
					return mNlogLog.IsFatalEnabled;
				case StakhanoviseLogLevel.Info:
					return mNlogLog.IsInfoEnabled;
				case StakhanoviseLogLevel.Trace:
					return mNlogLog.IsTraceEnabled;
				case StakhanoviseLogLevel.Warn:
					return mNlogLog.IsWarnEnabled;
				default:
					return false;
			}
		}

		public void Trace ( string message )
		{
			mNlogLog.Trace( message );
		}

		public void TraceFormat ( string messageFormat, params object[] args )
		{
			mNlogLog.Trace( messageFormat, args );
		}

		public void Warn ( string message )
		{
			mNlogLog.Warn( message );
		}

		public void Warn ( string message, Exception exception )
		{
			mNlogLog.Warn( exception, message );
		}

		public void WarnFormat ( string message, params object[] args )
		{
			mNlogLog.Warn( message, args );
		}
	}
}
