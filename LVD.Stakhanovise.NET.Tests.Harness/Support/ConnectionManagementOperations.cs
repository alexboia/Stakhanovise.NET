using LVD.Stakhanovise.NET.Tests.Helpers;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ConnectionManagementOperations
	{
		private string mConnectionString;

		public ConnectionManagementOperations( string connectionString )
		{
			mConnectionString = connectionString;
		}

		public Task WaitAndTerminateConnectionAsync( int pid, ManualResetEvent syncHandle, int delayMilliseconds )
		{
			return Task.Run( async () =>
			{
				using ( NpgsqlConnection mgmtConn = new NpgsqlConnection( mConnectionString ) )
				{
					await mgmtConn.WaitAndTerminateConnectionAsync( pid,
						syncHandle,
						delayMilliseconds );
				}
			} );
		}
	}
}
