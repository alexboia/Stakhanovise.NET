using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class NoOpLoggingProvider : IStakhanoviseLoggingProvider
	{
		public IStakhanoviseLogger CreateLogger ( string name )
		{
			return NoOpLogger.Instance;
		}
	}
}
