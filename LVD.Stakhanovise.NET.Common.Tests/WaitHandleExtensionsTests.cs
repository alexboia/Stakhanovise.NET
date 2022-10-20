using LVD.Stakhanovise.NET.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Common.Tests
{
	[TestFixture]
	public class WaitHandleExtensionsTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanConvertToTask_NotSignaled_WithMaxTimeout()
		{
			ManualResetEvent waitHandle =
				new ManualResetEvent( false );

			Task whTask = waitHandle
				.ToTask();

			Assert.NotNull( whTask );
			Assert.AreEqual( false, whTask.IsCompleted );

			Task.Delay( 1000 )
				.ContinueWith( d => waitHandle.Set() )
				.Wait();

			whTask.Wait();
			Assert.AreEqual( true, whTask.IsCompleted );
		}

		[Test]
		[TestCase( 100, 200 )]
		[TestCase( 100, 500 )]
		[TestCase( 1000, 1500 )]
		[TestCase( 1000, 2500 )]
		[Repeat( 5 )]
		public void Test_CanConvertToTask_NotSignaled_TimesOut( int timeoutMilliseconds, int checkAfterTimeout )
		{
			ManualResetEvent waitHandle =
				new ManualResetEvent( false );

			Task whTask = waitHandle
				.ToTask( timeoutMilliseconds );

			Assert.NotNull( whTask );
			Assert.AreEqual( false, whTask.IsCompleted );

			Task.Delay( checkAfterTimeout )
				.Wait();

			whTask.Wait();
			Assert.AreEqual( true, whTask.IsCompleted );
		}

		[Test]
		public void Test_CanConvertToTask_Signaled()
		{
			ManualResetEvent waitHandle =
				new ManualResetEvent( true );

			Task whTask = waitHandle
				.ToTask();

			Assert.NotNull( whTask );

			whTask.Wait();
			Assert.AreEqual( true, whTask.IsCompleted );
		}

		private TimeSpan NoTimeout()
		{
			return TimeSpan.FromMilliseconds( 0 );
		}
	}
}
