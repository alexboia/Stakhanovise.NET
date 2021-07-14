using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public static class ConnectionOptionsDefaults
	{
		public const int MaxRetryCount = 3;

		public const int RetryDelayMilliseconds = 100;

		public const int KeepAliveSeconds = 0;
	}
}
