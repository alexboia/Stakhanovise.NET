using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class NoOpLogger : IStakhanoviseLogger
	{
		public static readonly NoOpLogger Instance = new NoOpLogger();

		private NoOpLogger()
		{
			return;
		}
		public void Trace ( string message )
		{
			return;
		}

		public void TraceFormat ( string messageFormat, params object[] args )
		{
			return;
		}


		public void Debug ( string message )
		{
			return;
		}

		public void DebugFormat ( string messageFormat, params object[] args )
		{
			return;
		}

		public void Error ( string message )
		{
			return;
		}

		public void Error ( string message, Exception exception )
		{
			return;
		}

		public void Fatal ( string message )
		{
			return;
		}

		public void Fatal ( string message, Exception exception )
		{
			return;
		}

		public void Info ( string message )
		{
			return;
		}

		public void InfoFormat ( string messageFormat, params object[] args )
		{
			return;
		}

		public void Warn ( string message )
		{
			return;
		}

		public void Warn ( string message, Exception exception )
		{
			return;
		}

		public void WarnFormat ( string message, params object[] args )
		{
			return;
		}

		public bool IsEnabled ( StakhanoviseLogLevel level )
		{
			return true;
		}
	}
}
