using LVD.Stakhanovise.NET.Tests.Helpers;
using Npgsql;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class BaseDbTests : BaseTestWithConfiguration
	{
		protected async Task<NpgsqlConnection> OpenDbConnectionAsync ( string connectionString )
		{
			NpgsqlConnection db = new NpgsqlConnection( connectionString );
			await db.OpenAsync();
			return db;
		}

		protected Task WaitAndTerminateConnectionAsync ( int pid, ManualResetEvent syncHandle, int timeout )
		{
			return Task.Run( async () =>
			{
				using ( NpgsqlConnection mgmtConn = new NpgsqlConnection( ManagementConnectionString ) )
				{
					await mgmtConn.WaitAndTerminateConnectionAsync( pid,
						syncHandle,
						timeout );
				}
			} );
		}

		private string ManagementConnectionString
			=> GetConnectionString( "mgmtDbConnectionString" );
	}
}
