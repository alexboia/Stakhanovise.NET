using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class PostgreSqlExecutionPerformanceMonitorWriterOptions
	{
		public PostgreSqlExecutionPerformanceMonitorWriterOptions ( ConnectionOptions connectionOptions )
		{
			ConnectionOptions = connectionOptions 
				?? throw new ArgumentNullException( nameof( connectionOptions ) );
		}

		public ConnectionOptions ConnectionOptions { get; private set; }
	}
}
