using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class PostgreSqlExecutionPerformanceMonitorWriterOptions
	{
		public PostgreSqlExecutionPerformanceMonitorWriterOptions ( string connectionString, 
			int connectionRetryDelay, 
			int connectionRetryCount )
		{
			ConnectionString = connectionString 
				?? throw new ArgumentNullException( nameof( connectionString ) );

			ConnectionRetryCount = connectionRetryCount;
			ConnectionRetryDelay = connectionRetryDelay;
		}

		public string ConnectionString { get; private set; }

		public int ConnectionRetryCount { get; private set; }

		public int ConnectionRetryDelay { get; private set; }
	}
}
