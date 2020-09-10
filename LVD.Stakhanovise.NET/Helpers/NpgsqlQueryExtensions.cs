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
using Dapper;
using Npgsql;
using NpgsqlTypes;
using LVD.Stakhanovise.NET.Infrastructure;
using SqlKata.Execution;
using SqlKata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NpgsqlQueryExtensions
	{
		private static ConcurrentDictionary<string, bool> mListeningChannels
			= new ConcurrentDictionary<string, bool>();

		public static QueryFactory QueryFactory ( this NpgsqlConnection db )
		{
			return new QueryFactory( db, new PostgresCompilerEx() );
		}

		public static bool IsConnectionSomewhatOpen ( this NpgsqlConnection db )
		{
			return db != null && ( db.State == ConnectionState.Open
				|| db.State == ConnectionState.Executing
				|| db.State == ConnectionState.Fetching );
		}

		public static async Task UnlockAllAsync ( this NpgsqlConnection db )
		{
			using ( NpgsqlCommand cmd = new NpgsqlCommand( "SELECT pg_advisory_unlock_all()", db ) )
			{
				cmd.CommandType = CommandType.Text;
				await cmd.ExecuteNonQueryAsync();
			}
		}

		public static async Task<bool> Unlock ( this NpgsqlConnection db, long lockHandleId )
		{
			using ( NpgsqlCommand cmd = new NpgsqlCommand( "SELECT pg_advisory_unlock(@lock_handle_id)", db ) )
			{
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.AddWithValue( "lock_handle_id",
					NpgsqlDbType.Bigint,
					lockHandleId );

				return ( bool )( await cmd.ExecuteScalarAsync() );
			}
		}

		public static NpgsqlCommand ToNpgsqlCommand ( this Query query )
		{
			if ( query == null )
				throw new ArgumentNullException( nameof( query ) );

			SqlResult compiledQuery = new PostgresCompilerEx()
				.Compile( query );

			NpgsqlCommand compiledCommand = new NpgsqlCommand( compiledQuery.Sql );

			if ( compiledQuery.NamedBindings != null )
			{
				foreach ( KeyValuePair<string, object> paramWithValue in compiledQuery.NamedBindings )
					compiledCommand.Parameters.AddWithValue( paramWithValue.Key,
						paramWithValue.Value );
			}

			return compiledCommand;
		}

		public static async Task<NpgsqlDataReader> ExecuteReaderAsync ( this NpgsqlConnection db, Query query )
		{
			if ( db == null )
				throw new ArgumentNullException( nameof( db ) );

			if ( query == null )
				throw new ArgumentNullException( nameof( query ) );

			using ( NpgsqlCommand cmd = query.ToNpgsqlCommand() )
			{
				cmd.Connection = db;
				return ( NpgsqlDataReader )( await cmd.ExecuteReaderAsync() );
			}
		}

		public static async Task<int> ExecuteNonQueryAsync ( this NpgsqlConnection db, Query query )
		{
			if ( db == null )
				throw new ArgumentNullException( nameof( db ) );

			if ( query == null )
				throw new ArgumentNullException( nameof( query ) );

			using ( NpgsqlCommand cmd = query.ToNpgsqlCommand() )
			{
				cmd.Connection = db;
				return await cmd.ExecuteNonQueryAsync();
			}
		}

		public static async Task<object> ExecuteScalarAsync ( this NpgsqlConnection db, Query query )
		{
			if ( db == null )
				throw new ArgumentNullException( nameof( db ) );

			if ( query == null )
				throw new ArgumentNullException( nameof( query ) );

			using ( NpgsqlCommand cmd = query.ToNpgsqlCommand() )
			{
				cmd.Connection = db;
				return await cmd.ExecuteScalarAsync();
			}
		}

		public static async Task<int> InsertAsync ( this Query query, IReadOnlyDictionary<string, object> insertValues, NpgsqlTransaction withinTx )
		{
			XQuery xQuery = query as XQuery;

			if ( xQuery == null )
				throw new ArgumentNullException( nameof( query ), "Query is null or not of XQuery type" );

			SqlResult compiled = xQuery.Compiler
				.Compile( query.AsInsert( insertValues ) );

			xQuery.Logger( compiled );

			return await xQuery.Connection.ExecuteAsync( compiled.Sql,
				param: compiled.NamedBindings,
				transaction: withinTx );
		}

		public static async Task NotifyAsync ( this NpgsqlConnection db, string channel, NpgsqlTransaction withinTx )
		{
			if ( db == null )
				return;

			if ( string.IsNullOrEmpty( channel ) )
				throw new ArgumentNullException( nameof( channel ) );

			using ( NpgsqlCommand notifyCmd = new NpgsqlCommand( $"NOTIFY {channel}", db ) )
			{
				if ( withinTx != null )
					notifyCmd.Transaction = withinTx;
				await notifyCmd.ExecuteNonQueryAsync();
			}
		}

		public static async Task<bool> ListenAsync ( this NpgsqlConnection db, string channel, NotificationEventHandler eventHandler )
		{
			if ( db == null )
				return false;

			if ( string.IsNullOrEmpty( channel ) )
				throw new ArgumentNullException( nameof( channel ) );

			if ( eventHandler != null )
				db.Notification += eventHandler;

			if ( !db.IsConnectionSomewhatOpen() )
				return false;

			using ( NpgsqlCommand listenCmd = new NpgsqlCommand( $"LISTEN {channel}", db ) )
				await listenCmd.ExecuteNonQueryAsync();

			mListeningChannels.TryAdd( channel, true );
			return true;
		}

		public static async Task<bool> UnlistenAsync ( this NpgsqlConnection db, string channel, NotificationEventHandler eventHandler )
		{
			if ( db == null )
				return false;

			if ( string.IsNullOrEmpty( channel ) )
				throw new ArgumentNullException( nameof( channel ) );

			if ( eventHandler != null )
				db.Notification -= eventHandler;

			bool isListening;
			if ( mListeningChannels.TryRemove( channel, out isListening ) && isListening )
			{
				if ( !db.IsConnectionSomewhatOpen() )
					return false;

				using ( NpgsqlCommand unlistenCmd = new NpgsqlCommand( $"UNLISTEN {channel}", db ) )
					await unlistenCmd.ExecuteNonQueryAsync();
				return true;
			}
			else
				return false;
		}

		public static async Task<bool> IsAdvisoryLockHeldAsync ( this NpgsqlConnection db, long lockHandleId )
		{
			if ( db == null )
				throw new ArgumentNullException( nameof( db ) );

			using ( NpgsqlCommand checkCmd = new NpgsqlCommand( "SELECT sk_has_advisory_lock(@lock_handle_id) AS is_lock_held", db ) )
			{
				checkCmd.Parameters.AddWithValue( "lock_handle_id",
					parameterType: NpgsqlDbType.Bigint,
					value: lockHandleId );

				object result = await checkCmd.ExecuteScalarAsync();
				return result is bool && ( bool )result;
			}
		}

		public static bool IsListening ( this NpgsqlConnection db, string channel )
		{
			if ( db == null )
				return false;

			if ( string.IsNullOrEmpty( channel ) )
				throw new ArgumentNullException( nameof( channel ) );

			bool isListening;
			if ( !mListeningChannels.TryGetValue( channel, out isListening ) )
				isListening = false;

			return isListening;
		}
	}
}
