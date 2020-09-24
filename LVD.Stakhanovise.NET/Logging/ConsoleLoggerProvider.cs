using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class ConsoleLoggerProvider : IStakhanoviseLoggingProvider
	{
		private StakhanoviseLogLevel mMinLevel;

		private bool mWriteToStdOut = false;

		public ConsoleLoggerProvider( StakhanoviseLogLevel minLevel, bool writeToStdOut = false )
		{
			mMinLevel = minLevel;
			mWriteToStdOut = writeToStdOut;
		}

		public IStakhanoviseLogger CreateLogger ( string name )
		{
			return new ConsoleLogger( mMinLevel, name, mWriteToStdOut );
		}
	}
}
