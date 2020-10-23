using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Options
{
	public class PostgreSqlAppMetricsMonitorWriterOptions
	{
		public PostgreSqlAppMetricsMonitorWriterOptions ( ConnectionOptions connectionOptions, QueuedTaskMapping mapping )
		{
			ConnectionOptions = connectionOptions
				?? throw new ArgumentNullException( nameof( connectionOptions ) );
			Mapping = mapping
				?? throw new ArgumentNullException( nameof( mapping ) );
		}

		public ConnectionOptions ConnectionOptions { get; private set; }

		public QueuedTaskMapping Mapping { get; private set; }
	}
}
