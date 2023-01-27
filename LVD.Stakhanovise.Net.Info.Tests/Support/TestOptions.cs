using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.Net.Info.Tests.Support
{
	public class TestOptions : CommonTestOptions
	{
		public static TaskQueueInfoOptions GetDefaultTaskQueueInfoOptions( string connectionString )
		{
			return new TaskQueueInfoOptions( GetConnectionOptions( connectionString, keepAliveSeconds: 0 ),
				mapping: DefaultMapping );
		}
	}
}
