using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.Tests.Harness
{
	public static class LogMessageTestsHelperExtensions
	{
		public static void CallDebug( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logMessageExpectations,
			bool withFormattedMessage )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logMessageExpectations == null )
				throw new ArgumentNullException( nameof( logMessageExpectations ) );

			if ( withFormattedMessage )
				skLogger.DebugFormat( logMessageExpectations.ExpectedMessage, logMessageExpectations.ExpectedMessageArgs );
			else
				skLogger.Debug( logMessageExpectations.ExpectedMessage );
		}

		public static void CallError( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logExceptionExpectations,
			bool withException )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logExceptionExpectations == null )
				throw new ArgumentNullException( nameof( logExceptionExpectations ) );

			if ( withException )
				skLogger.Error( logExceptionExpectations.ExpectedMessage, logExceptionExpectations.ExpectedException );
			else
				skLogger.Error( logExceptionExpectations.ExpectedMessage );
		}

		public static void CallFatal( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logFatalExpectations,
			bool withException )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logFatalExpectations == null )
				throw new ArgumentNullException( nameof( logFatalExpectations ) );

			if ( withException )
				skLogger.Fatal( logFatalExpectations.ExpectedMessage, logFatalExpectations.ExpectedException );
			else
				skLogger.Fatal( logFatalExpectations.ExpectedMessage );
		}

		public static void CallInfo( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logMessageExpectations,
			bool withFormattedMessage )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logMessageExpectations == null )
				throw new ArgumentNullException( nameof( logMessageExpectations ) );

			if ( withFormattedMessage )
				skLogger.InfoFormat( logMessageExpectations.ExpectedMessage, logMessageExpectations.ExpectedMessageArgs );
			else
				skLogger.Info( logMessageExpectations.ExpectedMessage );
		}

		public static void CallTrace( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logMessageExpectations,
			bool withFormattedMessage )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logMessageExpectations == null )
				throw new ArgumentNullException( nameof( logMessageExpectations ) );

			if ( withFormattedMessage )
				skLogger.TraceFormat( logMessageExpectations.ExpectedMessage, logMessageExpectations.ExpectedMessageArgs );
			else
				skLogger.Trace( logMessageExpectations.ExpectedMessage );
		}

		public static void CallWarnWithoutException( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logWarnExpectations,
			bool withFormattedMessage )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logWarnExpectations == null )
				throw new ArgumentNullException( nameof( logWarnExpectations ) );

			if ( withFormattedMessage )
				skLogger.WarnFormat( logWarnExpectations.ExpectedMessage, logWarnExpectations.ExpectedMessageArgs );
			else
				skLogger.Warn( logWarnExpectations.ExpectedMessage );
		}

		public static void CallWarnWitException( this IStakhanoviseLogger skLogger,
			LogMessageExpectations logWarnExpectations )
		{
			if ( skLogger == null )
				throw new ArgumentNullException( nameof( skLogger ) );

			if ( logWarnExpectations == null )
				throw new ArgumentNullException( nameof( logWarnExpectations ) );

			skLogger.Warn( logWarnExpectations.ExpectedMessage,
				logWarnExpectations.ExpectedException );
		}
	}
}
