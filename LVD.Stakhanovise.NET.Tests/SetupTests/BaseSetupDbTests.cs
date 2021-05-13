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
using NUnit.Framework;
using Npgsql;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Tests.Support;
using Bogus;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public abstract class BaseSetupDbTests : BaseDbTests
	{
		private const string SetupTestDbName = "lvd_stakhanovise_setup_test_db";

		[SetUp]
		public async Task SetUp ()
		{
			try
			{
				NpgsqlConnection.ClearAllPools();
			}
			finally
			{
				await DropDatabaseAsync( SetupTestDbName );
				await CreateDatabaseAsync( SetupTestDbName );
			}

		}

		[TearDown]
		public async Task TearDown ()
		{
			try
			{
				NpgsqlConnection.ClearAllPools();
			}
			finally
			{
				await TerminateConnectionsToDbAsync( SetupTestDbName );
				await DropDatabaseAsync( SetupTestDbName );
			}
		}

		protected async Task CreateDatabaseAsync ( string dbName )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ServerDbConnectionString ) )
			{
				string dbCreationSql = $"CREATE DATABASE {dbName}";
				using ( NpgsqlCommand cmd = new NpgsqlCommand( dbCreationSql, conn ) )
					await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		protected async Task DropDatabaseAsync ( string dbName )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ServerDbConnectionString ) )
			{
				string dbCreationSql = $"DROP DATABASE IF EXISTS {dbName}";
				using ( NpgsqlCommand cmd = new NpgsqlCommand( dbCreationSql, conn ) )
					await cmd.ExecuteNonQueryAsync();
				await conn.CloseAsync();
			}
		}

		protected ConnectionOptions GetSetupTestDbConnectionOptions ()
		{
			return TestOptions.GetDefaultConnectionOptions( GetSetupTestDbConnectionString() );
		}

		protected async Task<bool> TableExistsAsync ( string tableName )
		{
			bool tableExists = false;

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( GetSetupTestDbConnectionString() ) )
			{
				string checkSql = $"SELECT to_regclass('{tableName}') IS NOT NULL AS table_exists";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( checkSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
						tableExists = rdr.GetBoolean( 0 );
				}

				await conn.CloseAsync();
			}

			return tableExists;
		}

		protected async Task<bool> TableHasColumnsAsync ( string tableName, params string[] expectedColumnNames )
		{
			bool tableHasColumns = false;
			Dictionary<string, string> columns = await GetTableColumnsAsync( tableName );

			if ( columns.Count == expectedColumnNames.Length )
			{
				tableHasColumns = true;
				foreach ( string expectedColumnName in expectedColumnNames )
				{
					if ( !columns.ContainsKey( expectedColumnName.ToLower() ) )
					{
						tableHasColumns = false;
						break;
					}
				}
			}

			return tableHasColumns;
		}

		protected async Task<bool> TableIndexExistsAsync ( string tableName, string indexName )
		{
			bool indexExists = false;

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( GetSetupTestDbConnectionString() ) )
			{
				string checkIndexSql = $@"SELECT COUNT(1) AS index_count 
					FROM pg_indexes
					WHERE tablename = '{tableName}' 
					AND indexname = '{indexName}'";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( checkIndexSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
						indexExists = rdr.GetInt64( 0 ) == 1;
				}

				await conn.CloseAsync();
			}

			return indexExists;
		}

		private async Task<Dictionary<string, string>> GetTableColumnsAsync ( string tableName )
		{
			Dictionary<string, string> columns =
				new Dictionary<string, string>();

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( GetSetupTestDbConnectionString() ) )
			{
				string getColumnsSql = $@"SELECT column_name, data_type 
					FROM information_schema.columns 
					WHERE table_catalog = '{SetupTestDbName}' 
						AND table_name = '{tableName}';";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( getColumnsSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					while ( await rdr.ReadAsync() )
					{
						string columnName = rdr.GetString( rdr
							.GetOrdinal( "column_name" ) );
						string dataType = rdr.GetString( rdr
							.GetOrdinal( "data_type" ) );

						columns.Add( columnName.ToLower(),
							dataType.ToUpper() );
					}
				}

				await conn.CloseAsync();
			}

			return columns;
		}

		protected async Task<bool> SequenceExistsAsync ( string sequenceName )
		{
			bool sequenceExists = false;

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( GetSetupTestDbConnectionString() ) )
			{
				string checkSequenceSql = $@"SELECT COUNT(1) AS seq_count 
					FROM pg_sequences 
					WHERE sequencename = '{sequenceName}'";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( checkSequenceSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
						sequenceExists = rdr.GetInt64( 0 ) > 0;
				}
			}

			return sequenceExists;
		}

		protected async Task<bool> PgFunctionExists ( string functionName, Dictionary<string, char> expectedParams )
		{
			bool functionExists = false;

			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( GetSetupTestDbConnectionString() ) )
			{
				string checkFunctionSql = $@"SELECT proargnames, proargmodes
					FROM pg_proc 
					WHERE proname = '{functionName}'
						AND prokind = 'f'";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( checkFunctionSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					if ( await rdr.ReadAsync() )
					{
						string[] actualArgNames = rdr.GetValue( rdr.GetOrdinal( "proargnames" ) )
							as string[];
						char[] actualArgModes = rdr.GetValue( rdr.GetOrdinal( "proargmodes" ) )
							as char[];

						if ( actualArgNames.Length == expectedParams.Count )
						{
							functionExists = true;
							for ( int i = 0; i < actualArgNames.Length; i++ )
							{
								string actualArgName = actualArgNames[ i ];
								char actualArgMode = actualArgModes[ i ];

								if ( !expectedParams.ContainsKey( actualArgName ) 
									|| expectedParams[ actualArgName ] != actualArgMode )
								{
									functionExists = false;
									break;
								}
							}
						}
					}
				}
			}

			return functionExists;
		}

		protected string RandomizeDbAssetName ( string tableName )
		{
			Faker faker = new Faker();
			tableName = faker.Lorem.Letter( 5 ) + "_" + tableName;
			return tableName;
		}

		private string GetSetupTestDbConnectionString ()
		{
			string serverDbConnectionString = ServerDbConnectionString;
			if ( !serverDbConnectionString.EndsWith( ';' ) )
				serverDbConnectionString += ";";
			return serverDbConnectionString + "Database=" + SetupTestDbName;
		}

		private async Task TerminateConnectionsToDbAsync ( string dbName )
		{
			using ( NpgsqlConnection conn = await OpenDbConnectionAsync( ServerDbConnectionString ) )
			{
				string terminateConnsSql = $@"SELECT pg_terminate_backend(pid) 
					FROM 
						pg_stat_activity 
					WHERE pid <> pg_backend_pid()
						AND datname = '{dbName}';";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( terminateConnsSql, conn ) )
					await cmd.ExecuteNonQueryAsync();

				await conn.CloseAsync();
			}
		}

		private string ServerDbConnectionString
			=> GetConnectionString( "serverDbConnectionString" );
	}
}
