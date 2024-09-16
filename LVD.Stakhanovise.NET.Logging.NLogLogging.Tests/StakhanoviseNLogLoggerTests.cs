using LVD.Stakhanovise.NET.Logging.Tests.Harness;
using Moq;
using NLog.Fluent;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging.Tests
{
	[TestFixture]
	public class StakhanoviseNLogLoggerTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanLogDebugMessage_WithoutFormat()
		{
			RunDebugLogMessageTests( withFormattedMessage: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogDebugMessage_WithFormat()
		{
			RunDebugLogMessageTests( withFormattedMessage: true );
		}

		private void RunDebugLogMessageTests( bool withFormattedMessage )
		{
			LogMessageExpectations logMessageExpectations =
				GenerateLogMessageExpectations( withFormattedMessage );

			TargetLoggerMockLoggerSetupProperties loggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.DebugEnabledWithExpectations( logMessageExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( loggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallDebug( logMessageExpectations,
				withFormattedMessage );

			targetLoggerMock.Verify();
		}

		private LogMessageExpectations GenerateLogMessageExpectations( bool formattedMessage )
		{
			return LogMessageExpectations.GenerateWithMessageOnly( formattedMessage );
		}

		[Test]
		public void Test_CanCheckIfDebugLogEnabled_WhenEnabled()
		{
			RunDebugLogEnabledCheckTests( withDebugLogEnabled: true );
		}

		[Test]
		public void Test_CanCheckIfDebugLogEnabled_WhenDisabled()
		{
			RunDebugLogEnabledCheckTests( withDebugLogEnabled: false );
		}

		private void RunDebugLogEnabledCheckTests( bool withDebugLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withDebugLogEnabled
				? TargetLoggerMockLoggerSetupProperties.DebugEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.DebugDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Debug,
				withDebugLogEnabled );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogError_WithoutException()
		{
			RunErrorLogMessageTests( withException: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogError_WithException()
		{
			RunErrorLogMessageTests( withException: true );
		}

		private void RunErrorLogMessageTests( bool withException )
		{
			LogMessageExpectations logExceptionExpectations = GenerateLogExceptionExpectations( withException,
				withFormattedMessage: false );

			TargetLoggerMockLoggerSetupProperties loggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.ErrorEnabledWithExpectations( logExceptionExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( loggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallError( logExceptionExpectations,
				withException );

			targetLoggerMock.Verify();
		}

		private LogMessageExpectations GenerateLogExceptionExpectations( bool withException, bool withFormattedMessage )
		{
			return withException
				? LogMessageExpectations.GenerateWithException( withFormattedMessage )
				: LogMessageExpectations.GenerateWithMessageOnly( withFormattedMessage );
		}

		[Test]
		public void Test_CanCheckIfErrorLogEnabled_WhenDisabled()
		{
			RunErrorLogEnabledCheckTests( withErrorLogEnabled: false );
		}

		[Test]
		public void Test_CanCheckIfErrorLogEnabled_WhenEnabled()
		{
			RunErrorLogEnabledCheckTests( withErrorLogEnabled: true );
		}

		private void RunErrorLogEnabledCheckTests( bool withErrorLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withErrorLogEnabled
				? TargetLoggerMockLoggerSetupProperties.ErrorEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.ErrorDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Error,
				withErrorLogEnabled );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogFatal_WithoutException()
		{
			RunFatalLogMessageTests( withException: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogFatal_WithException()
		{
			RunFatalLogMessageTests( withException: true );
		}

		private void RunFatalLogMessageTests( bool withException )
		{
			LogMessageExpectations logFatalExpectations = GenerateLogExceptionExpectations( withException,
				withFormattedMessage: false );

			TargetLoggerMockLoggerSetupProperties targetLoggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.FatalEnabledWithExpectations( logFatalExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( targetLoggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallFatal( logFatalExpectations,
				withException );

			targetLoggerMock.Verify();
		}

		[Test]
		public void Test_CanCheckIfFatalLogEnabled_WhenDisabled()
		{
			RunFatalLogEnabledCheckTests( withFatalLogEnabled: false );
		}

		[Test]
		public void Test_CanCheckIfFatalLogEnabled_WhenEnabled()
		{
			RunFatalLogEnabledCheckTests( withFatalLogEnabled: true );
		}

		private void RunFatalLogEnabledCheckTests( bool withFatalLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withFatalLogEnabled
				? TargetLoggerMockLoggerSetupProperties.FatalEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.FatalDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Fatal,
				withFatalLogEnabled );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogInfoMessage_WithoutFormat()
		{
			RunInfoLogMessageTests( withFormattedMessage: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogInfoMessage_WithFormat()
		{
			RunInfoLogMessageTests( withFormattedMessage: true );
		}

		private void RunInfoLogMessageTests( bool withFormattedMessage )
		{
			LogMessageExpectations logMessageExpectations =
				GenerateLogMessageExpectations( withFormattedMessage );

			TargetLoggerMockLoggerSetupProperties targetLoggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.InfoEnabledWithExpectations( logMessageExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( targetLoggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallInfo( logMessageExpectations,
				withFormattedMessage );

			targetLoggerMock.Verify();
		}

		[Test]
		public void Test_CanCheckIfInfoLogEnabled_WhenDisabled()
		{
			RunInfoLogEnabledCheckTests( withInfoLogEnabled: false );
		}

		[Test]
		public void Test_CanCheckIfInfoLogEnabled_WhenEnabled()
		{
			RunInfoLogEnabledCheckTests( withInfoLogEnabled: true );
		}

		private void RunInfoLogEnabledCheckTests( bool withInfoLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withInfoLogEnabled
				? TargetLoggerMockLoggerSetupProperties.InfoEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.InfoDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Info,
				withInfoLogEnabled );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogTraceMessage_WithoutFormat()
		{
			RunTraceLogMessageTests( withFormattedMessage: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogTraceMessage_WithFormat()
		{
			RunTraceLogMessageTests( withFormattedMessage: true );
		}

		private void RunTraceLogMessageTests( bool withFormattedMessage )
		{
			LogMessageExpectations logMessageExpectations =
				GenerateLogMessageExpectations( withFormattedMessage );

			TargetLoggerMockLoggerSetupProperties targetLoggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.TraceEnabledWithExpectations( logMessageExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( targetLoggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallTrace( logMessageExpectations,
				withFormattedMessage );

			targetLoggerMock.Verify();
		}

		[Test]
		public void Test_CanCheckIfTraceLogEnabled_WhenDisabled()
		{
			RunTraceLogEnabledCheckTests( withTraceLogEnabled: false );
		}

		[Test]
		public void Test_CanCheckIfTraceLogEnabled_WhenEnabled()
		{
			RunTraceLogEnabledCheckTests( withTraceLogEnabled: true );
		}

		private void RunTraceLogEnabledCheckTests( bool withTraceLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withTraceLogEnabled
				? TargetLoggerMockLoggerSetupProperties.TraceEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.TraceDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Trace,
				withTraceLogEnabled );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogWarn_WithoutFormat_WithoutException()
		{
			RunWarnLogMessageTestsWithoutException( withFormattedMessage: false );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogWarn_WithoutFormat_WithException()
		{
			RunWarnLogMessageTestsWithException();
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanLogWarn_WithFormat()
		{
			RunWarnLogMessageTestsWithoutException( withFormattedMessage: true );
		}

		private void RunWarnLogMessageTestsWithoutException( bool withFormattedMessage )
		{
			LogMessageExpectations logWarnExpectations = GenerateLogExceptionExpectations( withException: false,
				withFormattedMessage: withFormattedMessage );

			TargetLoggerMockLoggerSetupProperties loggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.WarnEnabledWithExpectations( logWarnExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( loggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallWarnWithoutException( logWarnExpectations,
				withFormattedMessage );

			targetLoggerMock.Verify();
		}

		private void RunWarnLogMessageTestsWithException()
		{
			LogMessageExpectations logWarnExpectations = GenerateLogExceptionExpectations( withException: true,
				withFormattedMessage: false );

			TargetLoggerMockLoggerSetupProperties loggerSetupProps = TargetLoggerMockLoggerSetupProperties
				.WarnEnabledWithExpectations( logWarnExpectations );

			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( loggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			stakhanoviseLogger.CallWarnWitException( logWarnExpectations );
			targetLoggerMock.Verify();
		}

		[Test]
		public void Test_CanCheckIfWarnEnabled_WhenEnabled()
		{
			RunWarnLogEnabledCheckTests( withWarnLogEnabled: true );
		}

		[Test]
		public void Test_CanCheckIfWarnEnabled_WhenDisabled()
		{
			RunWarnLogEnabledCheckTests( withWarnLogEnabled: false );
		}

		private void RunWarnLogEnabledCheckTests( bool withWarnLogEnabled )
		{
			TargetLoggerMockLoggerSetupProperties loggerSetupProps = withWarnLogEnabled
				? TargetLoggerMockLoggerSetupProperties.WarnEnabledWithoutExpectations()
				: TargetLoggerMockLoggerSetupProperties.WarnDisabled();

			RunLogLevelEnabledCheckTests( loggerSetupProps,
				StakhanoviseLogLevel.Warn,
				withWarnLogEnabled );
		}

		private void RunLogLevelEnabledCheckTests( TargetLoggerMockLoggerSetupProperties loggerSetupProps,
			StakhanoviseLogLevel logLevel,
			bool isLevelEnabled )
		{
			Mock<ILogger> targetLoggerMock =
				CreateNLogLoggerMock( loggerSetupProps );

			StakhanoviseNLogLogger stakhanoviseLogger =
				new StakhanoviseNLogLogger( targetLoggerMock.Object );

			if ( isLevelEnabled )
				ClassicAssert.IsTrue( stakhanoviseLogger.IsEnabled( logLevel ) );
			else
				ClassicAssert.IsFalse( stakhanoviseLogger.IsEnabled( logLevel ) );
		}

		private Mock<ILogger> CreateNLogLoggerMock( TargetLoggerMockLoggerSetupProperties props )
		{
			Mock<ILogger> mock = new Mock<ILogger>( MockBehavior.Strict );

			mock = SetupDebugForLoggerMock( mock, props );
			mock = SetupErrorForLoggerMock( mock, props );
			mock = SetupFatalForLoggerMock( mock, props );
			mock = SetupInfoForLoggerMock( mock, props );
			mock = SetupTraceForLoggerMock( mock, props );
			mock = SetupWarnForLoggerMock( mock, props );

			return mock;
		}

		private Mock<ILogger> SetupDebugForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsDebugEnabled )
			{
				if ( props.DebugMessageExpectations != null )
				{
					string expectedMessage = props.DebugMessageExpectations.ExpectedMessage;
					if ( props.DebugMessageExpectations.ExpectedMessageArgs != null )
						mock.Setup( m => m.Debug( expectedMessage, ( object [] ) props.DebugMessageExpectations.ExpectedMessageArgs ) )
							.Verifiable();
					else
						mock.Setup( m => m.Debug( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Debug( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Debug( It.IsAny<string>(), It.IsAny<object []>() ) )
						.Verifiable();
				};
			}

			mock.SetupGet( m => m.IsDebugEnabled )
				.Returns( props.IsDebugEnabled );

			return mock;
		}

		private Mock<ILogger> SetupErrorForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsErrorEnabled )
			{
				if ( props.ErrorMessageExpectations != null )
				{
					string expectedMessage = props.ErrorMessageExpectations.ExpectedMessage;
					if ( props.ErrorMessageExpectations.ExpectedException != null )
						mock.Setup( m => m.Error( props.ErrorMessageExpectations.ExpectedException, expectedMessage ) )
							.Verifiable();
					else
						mock.Setup( m => m.Error( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Error( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Error( It.IsAny<Exception>(), It.IsAny<string>() ) )
						.Verifiable();
				}
			}

			mock.SetupGet( m => m.IsErrorEnabled )
				.Returns( props.IsErrorEnabled );

			return mock;
		}

		private Mock<ILogger> SetupFatalForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsFatalEnabled )
			{
				if ( props.FatalMessageExpectations != null )
				{
					string expectedMessage = props.FatalMessageExpectations.ExpectedMessage;
					if ( props.FatalMessageExpectations.ExpectedException != null )
						mock.Setup( m => m.Fatal( props.FatalMessageExpectations.ExpectedException, expectedMessage ) )
							.Verifiable();
					else
						mock.Setup( m => m.Fatal( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Fatal( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Fatal( It.IsAny<Exception>(), It.IsAny<string>() ) )
						.Verifiable();
				}
			}

			mock.SetupGet( m => m.IsFatalEnabled )
				.Returns( props.IsFatalEnabled );

			return mock;
		}

		private Mock<ILogger> SetupInfoForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsInfoEnabled )
			{
				if ( props.InfoMessageExpectations != null )
				{
					string expectedMessage = props.InfoMessageExpectations.ExpectedMessage;
					if ( props.InfoMessageExpectations.ExpectedMessageArgs != null )
						mock.Setup( m => m.Info( expectedMessage, ( object [] ) props.InfoMessageExpectations.ExpectedMessageArgs ) )
							.Verifiable();
					else
						mock.Setup( m => m.Info( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Info( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Info( It.IsAny<string>(), It.IsAny<object []>() ) )
						.Verifiable();
				};
			}

			mock.SetupGet( m => m.IsInfoEnabled )
				.Returns( props.IsInfoEnabled );

			return mock;
		}

		private Mock<ILogger> SetupTraceForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsTraceEnabled )
			{
				if ( props.TraceMessageExpectations != null )
				{
					string expectedMessage = props.TraceMessageExpectations.ExpectedMessage;
					if ( props.TraceMessageExpectations.ExpectedMessageArgs != null )
						mock.Setup( m => m.Trace( expectedMessage, ( object [] ) props.TraceMessageExpectations.ExpectedMessageArgs ) )
							.Verifiable();
					else
						mock.Setup( m => m.Trace( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Trace( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Trace( It.IsAny<string>(), It.IsAny<object []>() ) )
						.Verifiable();
				};
			}

			mock.SetupGet( m => m.IsTraceEnabled )
				.Returns( props.IsTraceEnabled );

			return mock;
		}

		private Mock<ILogger> SetupWarnForLoggerMock( Mock<ILogger> mock, TargetLoggerMockLoggerSetupProperties props )
		{
			if ( props.IsWarnEnabled )
			{
				if ( props.WarnMessageExpectations != null )
				{
					string expectedMessage = props.WarnMessageExpectations.ExpectedMessage;
					if ( props.WarnMessageExpectations.ExpectedMessageArgs != null )
						mock.Setup( m => m.Warn( expectedMessage, ( object [] ) props.WarnMessageExpectations.ExpectedMessageArgs ) )
							.Verifiable();
					else if ( props.WarnMessageExpectations.ExpectedException != null )
						mock.Setup( m => m.Warn( props.WarnMessageExpectations.ExpectedException, expectedMessage ) )
							.Verifiable();
					else
						mock.Setup( m => m.Warn( expectedMessage ) )
							.Verifiable();
				}
				else
				{
					mock.Setup( m => m.Warn( It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Warn( It.IsAny<Exception>(), It.IsAny<string>() ) )
						.Verifiable();
					mock.Setup( m => m.Warn( It.IsAny<string>(), It.IsAny<object []>() ) )
						.Verifiable();
				};
			}

			mock.SetupGet( m => m.IsWarnEnabled )
				.Returns( props.IsWarnEnabled );

			return mock;
		}
	}
}
