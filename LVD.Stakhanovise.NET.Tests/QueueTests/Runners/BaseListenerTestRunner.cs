using Bogus;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.QueueTests
{
	public abstract class BaseListenerTestRunner
	{
		private readonly ConnectionManagementOperations mConnectionOperations;

		public BaseListenerTestRunner( string managementDbConnectionString )
		{
			mConnectionOperations = new ConnectionManagementOperations( managementDbConnectionString );
		}

		public abstract Task RunTestsAsync( PostgreSqlTaskQueueNotificationListener listener );

		protected Task WaitAndTerminateConnectionAsync( int pid, ManualResetEvent syncHandle, int delayMilliseconds )
		{
			return mConnectionOperations.WaitAndTerminateConnectionAsync( pid,
				syncHandle,
				delayMilliseconds );
		}

		protected int RandomDelay()
		{
			return new Faker().Random
				.Int( 100, 2000 );
		}
	}
}