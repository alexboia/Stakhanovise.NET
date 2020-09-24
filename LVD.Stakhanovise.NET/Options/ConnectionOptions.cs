using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class ConnectionOptions
	{
		public ConnectionOptions ( string connectionString,
			int keepAlive,
			int retryCount = 3,
			int retryDelayMilliseconds = 100 )
		{
			ConnectionString = connectionString
				?? throw new ArgumentNullException( nameof( connectionString ) );

			ConnectionRetryCount = retryCount;
			ConnectionRetryDelayMilliseconds = retryDelayMilliseconds;
			ConnectionKeepAlive = keepAlive;
		}

		public int ConnectionRetryCount { get; private set; }

		public int ConnectionRetryDelayMilliseconds { get; private set; }

		public int ConnectionKeepAlive { get; private set; }

		public string ConnectionString { get; private set; }
	}
}
