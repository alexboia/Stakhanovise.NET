using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public interface IStakhanoviseLoggingProvider
	{
		IStakhanoviseLogger CreateLogger ( string name );
	}
}
