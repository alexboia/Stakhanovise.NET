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
			int delayMilliseconds )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			bool didOpen = false;

			if ( syncHandle != null )
				syncHandle.WaitOne();

			if ( delayMilliseconds > 0 )
				await Task.Delay( delayMilliseconds );

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
			int delayMilliseconds )
		{
			if ( mgmtConn == null )
				throw new ArgumentNullException( nameof( mgmtConn ) );

			if ( string.IsNullOrEmpty( appName ) )
				throw new ArgumentNullException( nameof( appName ) );

			bool didOpen = false;

			if ( syncHandle != null )
				syncHandle.WaitOne();

			if ( delayMilliseconds > 0 )
				await Task.Delay( delayMilliseconds );

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
