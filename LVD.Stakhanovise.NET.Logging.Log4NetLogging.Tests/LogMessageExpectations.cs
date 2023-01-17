using System;
using System.Collections.Generic;
using System.Text;
using Bogus;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging.Tests
{
	public class LogMessageExpectations
	{
		public static LogMessageExpectations GenerateWithMessageOnly( bool formattedMessage )
		{
			return formattedMessage
				? GenerateLogMessageExpectationsWithFormattedMessage()
				: GenerateLogMessageExpectationsWithRegularMessage();
		}

		public static LogMessageExpectations GenerateWithException( bool formattedMessage )
		{
			Faker faker = new Faker();
			LogMessageExpectations props =
				GenerateWithMessageOnly( formattedMessage );

			props.ExpectedException = faker.System
				.Exception();

			return props;
		}

		private static LogMessageExpectations GenerateLogMessageExpectationsWithFormattedMessage()
		{
			Faker faker = new Faker();
			LogMessageExpectations props = new LogMessageExpectations();

			int placeholderCount = faker.Random.Int( 0, 10 );
			if ( placeholderCount > 0 )
			{
				string [] words = faker.Lorem.Random
					.WordsArray( placeholderCount + 1 );

				props.ExpectedMessage =
					GenerateLogMessageFormat( words );

				props.ExpectedMessageArgs = faker.Lorem.Random
					.WordsArray( placeholderCount );
			}
			else
			{
				props.ExpectedMessage = faker.Lorem.Random.Words();
				props.ExpectedMessageArgs = LogMessageExpectations.NoMessageArgs;
			}

			return props;
		}

		private static LogMessageExpectations GenerateLogMessageExpectationsWithRegularMessage()
		{
			Faker faker = new Faker();
			return new LogMessageExpectations()
			{
				ExpectedMessage = faker.Lorem.Random.Words(),
				ExpectedMessageArgs = null
			};
		}

		private static string GenerateLogMessageFormat( string [] words )
		{
			string messageFormat = string.Empty;
			for ( int i = 0; i < words.Length; i++ )
			{
				messageFormat += words [ i ];
				if ( i < words.Length - 1 )
					messageFormat += $" {i + 1} ";
			}

			return messageFormat;
		}

		public string ExpectedMessage
		{
			get; set;
		}

		public string [] ExpectedMessageArgs
		{
			get; set;
		}

		public Exception ExpectedException
		{
			get; set;
		}

		public static string [] NoMessageArgs => new string [ 0 ];
	}
}
