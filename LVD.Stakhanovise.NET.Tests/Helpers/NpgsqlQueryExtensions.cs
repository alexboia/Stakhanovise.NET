using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class NpgsqlQueryExtensions
	{
		public static NpgsqlCommand ToNpgsqlCommand ( this Query query )
		{
			if ( query == null )
				throw new ArgumentNullException( nameof( query ) );

			SqlResult compiledQuery = new PostgresCompiler()
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
	}
}
