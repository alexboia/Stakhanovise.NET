using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class ConnectionManagementExtensions
	{
		public static async Task<List<int>> GetPidsForApplicationNames ( this NpgsqlConnection mgmtConn, string appName )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			if ( string.IsNullOrWhiteSpace( appName ) )
				throw new ArgumentNullException( nameof( appName ) );

			bool didOpen = false;
			List<int> pids = new List<int>();

			try
			{
				if ( mgmtConn.State != ConnectionState.Open )
				{
					await mgmtConn.OpenAsync();
					didOpen = true;
				}

				using ( NpgsqlCommand cmd = new NpgsqlCommand( "SELECT pid FROM pg_stat_activity WHERE application_name = @app_name", mgmtConn ) )
				{
					cmd.Parameters.AddWithValue( "app_name", NpgsqlDbType.Varchar, appName );
					using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
					{
						while ( await rdr.ReadAsync() )
							pids.Add( rdr.GetInt32( 0 ) );
					}
				}
			}
			finally
			{
				if ( didOpen )
					await mgmtConn.CloseAsync();
			}

			return pids;
		}

		public static async Task<bool> TerminateConnectionAsync ( this NpgsqlConnection mgmtConn, int pid )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			bool didOpen = false;
			bool result = false;

			try
			{
				if ( mgmtConn.State != ConnectionState.Open )
				{
					await mgmtConn.OpenAsync();
					didOpen = true;
				}

				using ( NpgsqlCommand cmd = new NpgsqlCommand( "SELECT pg_terminate_backend(@pid)", mgmtConn ) )
				{
					cmd.Parameters.AddWithValue( "pid",
						NpgsqlDbType.Integer,
						pid );

					result = ( bool )await cmd.ExecuteScalarAsync();
				}
			}
			finally
			{
				if ( didOpen )
					await mgmtConn.CloseAsync();
			}

			return result;
		}

		public static async Task WaitAndTerminateConnectionAsync ( this NpgsqlConnection mgmtConn,
			int pid,
			ManualResetEvent syncHandle,
			int timeout )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			bool didOpen = false;

			if ( syncHandle != null )
				syncHandle.WaitOne();

			if ( timeout > 0 )
				await Task.Delay( timeout );

			try
			{
				if ( mgmtConn.State != ConnectionState.Open )
				{
					await mgmtConn.OpenAsync();
					didOpen = true;
				}

				await mgmtConn.TerminateConnectionAsync( pid );
			}
			finally
			{
				if ( didOpen )
					await mgmtConn.CloseAsync();
			}
		}

		public static async Task WaitAndTerminateConnectionAsync ( this NpgsqlConnection mgmtConn,
			string appName,
			ManualResetEvent syncHandle,
			int timeout )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			if ( string.IsNullOrEmpty( appName ) )
				throw new ArgumentNullException( nameof( appName ) );

			bool didOpen = false;

			if ( syncHandle != null )
				syncHandle.WaitOne();

			if ( timeout > 0 )
				await Task.Delay( timeout );

			try
			{
				if ( mgmtConn.State != ConnectionState.Open )
				{
					await mgmtConn.OpenAsync();
					didOpen = true;
				}

				List<int> pids = await mgmtConn
					.GetPidsForApplicationNames( appName );

				foreach ( int pid in pids )
					await mgmtConn.TerminateConnectionAsync( pid );

			}
			finally
			{
				if ( didOpen )
					await mgmtConn.CloseAsync();
			}
		}
	}
}
