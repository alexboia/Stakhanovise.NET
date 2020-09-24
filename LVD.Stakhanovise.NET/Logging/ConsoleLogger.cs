using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class ConsoleLogger : IStakhanoviseLogger
	{
		private string mName;

		private StakhanoviseLogLevel mMinLevel;

		private bool mWriteToStdOut = false;

		public ConsoleLogger ( StakhanoviseLogLevel minLevel, string name, bool writeToStdOut = false )
		{
			mName = name;
			mMinLevel = minLevel;
			mWriteToStdOut = writeToStdOut;
		}

		private void Log ( StakhanoviseLogLevel level, string message, Exception exception = null )
		{
			if ( !IsEnabled( level ) )
				return;

			StringBuilder logMessage =
				new StringBuilder();

			string dateTime = DateTimeOffset.Now
				.ToString( "yyyy-MM-dd HH:mm:ss zzz" );

			logMessage.Append( $"[{dateTime}]" )
				.Append( " - " );

			if ( !string.IsNullOrEmpty( mName ) )
				logMessage.Append( $"{mName}" )
					.Append( " - " );

			logMessage.Append( $"{level.ToString()}" );

			if ( !string.IsNullOrEmpty( message ) )
				logMessage.Append( " - " ).Append( message );

			if ( exception != null )
			{
				logMessage
					.Append( " - " )
					.Append( exception.GetType().FullName )
					.Append( ": " )
					.Append( exception.Message )
					.Append( ":" )
					.Append( exception.StackTrace );
			}

			if ( !mWriteToStdOut )
				Console.Error.WriteLine( logMessage.ToString() );
			else
				Console.WriteLine( logMessage );
		}

		public void Trace ( string message )
		{
			Log( StakhanoviseLogLevel.Trace, message );
		}

		public void TraceFormat ( string messageFormat, params object[] args )
		{
			Trace( string.Format( messageFormat, args ) );
		}

		public void Debug ( string message )
		{
			Log( StakhanoviseLogLevel.Debug, message );
		}

		public void DebugFormat ( string messageFormat, params object[] args )
		{
			Debug( string.Format( messageFormat, args ) );
		}

		public void Error ( string message )
		{
			Log( StakhanoviseLogLevel.Error, message );
		}

		public void Error ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Error, message, exception );
		}

		public void Fatal ( string message )
		{
			Log( StakhanoviseLogLevel.Fatal, message );
		}

		public void Fatal ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Fatal, message, exception );
		}

		public void Info ( string message )
		{
			Log( StakhanoviseLogLevel.Info, message );
		}

		public void InfoFormat ( string messageFormat, params object[] args )
		{
			Info( string.Format( messageFormat, args ) );
		}

		public void Warn ( string message )
		{
			Log( StakhanoviseLogLevel.Warn, message );
		}

		public void Warn ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Warn, message, exception );
		}

		public void WarnFormat ( string messageFormat, params object[] args )
		{
			Warn( string.Format( messageFormat, args ) );
		}

		public bool IsEnabled ( StakhanoviseLogLevel level )
		{
			return level >= mMinLevel;
		}
	}
}
