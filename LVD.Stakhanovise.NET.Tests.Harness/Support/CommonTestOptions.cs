using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class CommonTestOptions
	{
		public static readonly int DefaultFaultErrorThresholdCount = 5;

		public static readonly QueuedTaskMapping DefaultMapping = new QueuedTaskMapping();

		public static ConnectionOptions GetConnectionOptions( string connectionString, int keepAliveSeconds )
		{
			return new ConnectionOptions( connectionString,
				keepAliveSeconds: keepAliveSeconds,
				retryCount: 3,
				retryDelayMilliseconds: 250 );
		}

		public static ConnectionOptions GetDefaultConnectionOptions( string connectionString )
		{
			return GetConnectionOptions( connectionString, 
				keepAliveSeconds: 0 );
		}

		public static TaskQueueOptions GetDefaultTaskQueueOptions( string connectionString )
		{
			return new TaskQueueOptions( GetConnectionOptions( connectionString, keepAliveSeconds: 0 ),
				DefaultMapping );
		}
	}
}
