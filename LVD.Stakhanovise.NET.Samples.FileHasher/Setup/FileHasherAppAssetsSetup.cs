﻿// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using System.Threading.Tasks;
using Npgsql;
using LVD.Stakhanovise.NET.Helpers;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Setup
{
	public class FileHasherAppAssetsSetup
	{
		private string mServerDbConnectionString;

		private string mFullDbConnectionString;

		private string mDbName;

		public FileHasherAppAssetsSetup( string connectionString )
		{
			NpgsqlConnectionStringBuilder sourceConnectionStringBuilder =
				new NpgsqlConnectionStringBuilder( connectionString );
			NpgsqlConnectionStringBuilder serverConnectionStringBuilder =
				sourceConnectionStringBuilder.Copy();

			serverConnectionStringBuilder.Database = null;
			mServerDbConnectionString = serverConnectionStringBuilder.ToString();

			mDbName = sourceConnectionStringBuilder.Database;
			mFullDbConnectionString = connectionString;
		}

		public async Task SetupAsync()
		{
			if ( !await DatabaseExistsAsync() )
				await CreateDatabaseAsync();
			else
				await TruncateAllTablesAsync();
		}

		private async Task<bool> DatabaseExistsAsync()
		{
			bool exists = false;

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( mServerDbConnectionString ) )
			{
				string dbCheckSql = $@"SELECT datname 
					FROM pg_catalog.pg_database 
					WHERE lower(datname) = lower('{mDbName}')";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( dbCheckSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
						exists = ( rdr.GetString( 0 ) == mDbName );
				}

				await conn.CloseAsync();
			}

			return exists;
		}

		private async Task CreateDatabaseAsync()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( mServerDbConnectionString ) )
			{
				string dbCreationSql = $"CREATE DATABASE {mDbName}";
				using ( NpgsqlCommand cmd = new NpgsqlCommand( dbCreationSql, conn ) )
					await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		private async Task TruncateAllTablesAsync()
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( mFullDbConnectionString ) )
			{
				List<string> allTableNames = await GetAllDbTableNamesAsync( conn );
				foreach ( string tableName in allTableNames )
				{
					string truncateTableSql = $"TRUNCATE TABLE {tableName}";
					using ( NpgsqlCommand cmd = new NpgsqlCommand( truncateTableSql, conn ) )
						await cmd.ExecuteNonQueryAsync();
				}
				await conn.CloseAsync();
			}
		}

		private async Task<List<string>> GetAllDbTableNamesAsync( NpgsqlConnection conn )
		{
			List<string> allTableNames =
				new List<string>();

			string getTablesSql = $@"SELECT tablename 
				FROM pg_tables 
				WHERE schemaname = 'public'";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( getTablesSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				while ( await rdr.ReadAsync() )
					allTableNames.Add( rdr.GetString( 0 ) );
			}

			return allTableNames;
		}

		private async Task<NpgsqlConnection> OpenDbConnectionAsync( string connectionString )
		{
			NpgsqlConnection db = new NpgsqlConnection( connectionString );
			await db.OpenAsync();
			return db;
		}
	}
}
