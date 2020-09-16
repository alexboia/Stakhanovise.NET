using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlConnectionExtensions
	{
		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this string connectionString, 
			int maxRetryCount = 3, 
			int retryDelay = 100 )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );
			
			int retryCount = 0;
			NpgsqlConnection conn = null;

			while ( retryCount < maxRetryCount )
			{
				try
				{
					conn = new NpgsqlConnection( connectionString );
					await conn.OpenAsync();
				}
				catch ( Exception )
				{
					conn = null;
					retryCount++;
					if ( retryCount > 0 )
						await Task.Delay( retryDelay );
				}
			}

			return conn;
		}
	}
}
