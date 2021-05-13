using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.SetupTests.Support
{
	public class ConnectionOptionsSourceData
	{
		public int ConnectionRetryCount { get; set; }

		public int ConnectionRetryDelayMilliseconds { get; set; }

		public int ConnectionKeepAliveSeconds { get; set; }

		public string ConnectionString { get; set; }
	}
}
