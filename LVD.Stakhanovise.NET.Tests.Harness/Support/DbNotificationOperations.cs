using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class DbNotificationOperations
	{
		private readonly string mConnectionString;

		public DbNotificationOperations( string connectionString )
		{
			mConnectionString = connectionString;
		}

		public void SendChannelNotification( string channelName )
		{
			SendChannelNotificationAsync( channelName )
				.Wait();
		}

		public async Task SendChannelNotificationAsync( string channelName )
		{
			using ( NpgsqlConnection db = await OpenDbConnectionAsync() )
			{
				await db.NotifyAsync( channelName, null );
				await db.CloseAsync();
			}

			await Task.Delay( 100 );
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync()
		{
			NpgsqlConnection db = new NpgsqlConnection( mConnectionString );
			await db.OpenAsync();
			return db;
		}
	}
}
