using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public interface IStakhanoviseLogger
	{
		void Debug ( string message );

		void DebugFormat ( string messageFormat, params object[] args );

		void Info ( string message );

		void InfoFormat ( string messageFormat, params object[] args );

		void Warn ( string message );

		void WarnFormat ( string message, params object[] args );

		void Warn ( string message, Exception exception );

		void Error ( string message );

		void Error ( string message, Exception exception );

		void Fatal ( string message );

		void Fatal ( string message, Exception exception );
	}
}
