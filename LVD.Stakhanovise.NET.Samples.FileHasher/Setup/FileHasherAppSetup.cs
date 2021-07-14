using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using LVD.Stakhanovise.NET.Helpers;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Setup
{
	public class FileHasherAppSetup
	{
		private string mServerDbConnectionString;

		private string mFullDbConnectionString;

		private string mDbName;

		public FileHasherAppSetup( string connectionString )
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
