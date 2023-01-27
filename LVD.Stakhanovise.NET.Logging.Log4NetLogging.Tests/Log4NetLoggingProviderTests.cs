using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Logging.Log4NetLogging.Tests
{
	[TestFixture]
	public class Log4NetLoggingProviderTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanCreateLogger ()
		{
			string name = Guid.NewGuid().ToString();
			Log4NetLoggingProvider provider = new Log4NetLoggingProvider();
			IStakhanoviseLogger logger = provider.CreateLogger( name );

			Assert.NotNull( logger );
			Assert.IsInstanceOf<Log4NetLogger>( logger );
		}
	}
}
