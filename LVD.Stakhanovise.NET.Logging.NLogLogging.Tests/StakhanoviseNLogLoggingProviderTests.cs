﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.NLogLogging.Tests
{
	[TestFixture]
	public class StakhanoviseNLogLoggingProviderTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanCreateLogger()
		{
			string name = Guid.NewGuid().ToString();
			StakhanoviseNLogLoggingProvider provider = new StakhanoviseNLogLoggingProvider();
			IStakhanoviseLogger logger = provider.CreateLogger( name );

			Assert.NotNull( logger );
			Assert.IsInstanceOf<StakhanoviseNLogLogger>( logger );
		}
	}
}
