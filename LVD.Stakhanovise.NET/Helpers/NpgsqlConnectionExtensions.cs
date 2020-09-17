using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlConnectionExtensions
	{
		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this string connectionString,
			int maxRetryCount = 3,
			int retryDelay = 100 )
		{
			return await connectionString.TryOpenConnectionAsync( CancellationToken.None,
				maxRetryCount,
				retryDelay );
		}

		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this string connectionString,
			CancellationToken cancellationToken,
			int maxRetryCount = 3,
			int retryDelay = 100 )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			int retryCount = 0;
			NpgsqlConnection conn = null;
			bool hasCancellation = !cancellationToken.Equals( CancellationToken.None );

			while ( retryCount < maxRetryCount )
			{
				if ( hasCancellation )
					cancellationToken.ThrowIfCancellationRequested();

				try
				{
					conn = new NpgsqlConnection( connectionString );
					if ( hasCancellation )
						await conn.OpenAsync( cancellationToken );
					else
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
