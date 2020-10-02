// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using LVD.Stakhanovise.NET.Options;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlConnectionExtensions
	{
		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this ConnectionOptions connectionOptions )
		{
			return await connectionOptions.TryOpenConnectionAsync( CancellationToken.None );
		}

		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this ConnectionOptions connectionOptions, CancellationToken cancellationToken )
		{
			if ( connectionOptions == null )
				throw new ArgumentNullException( nameof( connectionOptions ) );

			return await connectionOptions.ConnectionString
				.TryOpenConnectionAsync( cancellationToken,
					connectionOptions.ConnectionRetryCount,
					connectionOptions.ConnectionRetryDelayMilliseconds );
		}

		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this string connectionString,
			int maxRetryCount = 3,
			int retryDelayMilliseconds = 100 )
		{
			return await connectionString.TryOpenConnectionAsync( CancellationToken.None,
				maxRetryCount,
				retryDelayMilliseconds );
		}

		public static async Task<NpgsqlConnection> TryOpenConnectionAsync ( this string connectionString,
			CancellationToken cancellationToken,
			int maxRetryCount = 3,
			int retryDelay = 100 )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			if ( maxRetryCount < 1 )
				throw new ArgumentOutOfRangeException( nameof( maxRetryCount ),
					"Max retry count must be greater than 1" );

			if ( retryDelay < 1 )
				throw new ArgumentOutOfRangeException( nameof( retryDelay ),
					"Retry delay must be greater than 1" );

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

					break;
				}
				catch ( Exception exc )
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
