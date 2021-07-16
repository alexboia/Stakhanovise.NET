using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using Npgsql;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.Statistics
{
	public class DirectDbStatsProvider : IStatsProvider
	{
		private string mConnectionString;

		private QueuedTaskMapping mMapping;

		public DirectDbStatsProvider( string connectionString, QueuedTaskMapping mapping )
		{
			if ( string.IsNullOrWhiteSpace( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			mConnectionString = connectionString;
			mMapping = mapping;
		}

		public async Task<GenericCounts> ComputeGenericCountsAsync()
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			{
				return new GenericCounts()
				{
					TotalTasksInQueue = 
						await ComputeTotalTasksInQueueAsync( conn ),
					TotalResultsInResultQueue = 
						await ComputeTotalResultsInResultQueueAsync( conn ),
					TotalCompletedResultsInResultsQueue = 
						await ComputeTotalCompletedResultsInResultQueueAsync( conn )
				};
			}
		}

		private async Task<NpgsqlConnection> OpenConnectionAsync()
		{
			NpgsqlConnection conn = new NpgsqlConnection( mConnectionString );
			await conn.OpenAsync();
			return conn;
		}

		private async Task<long> ComputeTotalTasksInQueueAsync( NpgsqlConnection conn )
		{
			long total = 0;

			string countSql = @$"SELECT COUNT(1) AS total_cnt 
				FROM {mMapping.QueueTableName}";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				if ( await rdr.ReadAsync() )
					total = rdr.GetInt64( 0 );
			}

			return total;
		}

		private async Task<long> ComputeTotalResultsInResultQueueAsync( NpgsqlConnection conn )
		{
			long total = 0;

			string countSql = @$"SELECT COUNT(1) AS total_cnt 
				FROM {mMapping.ResultsQueueTableName}";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( $"SELECT COUNT(1) FROM {mMapping.ResultsQueueTableName}", conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				if ( await rdr.ReadAsync() )
					total = rdr.GetInt64( 0 );
			}

			return total;
		}

		private async Task<long> ComputeTotalCompletedResultsInResultQueueAsync( NpgsqlConnection conn )
		{
			long total = 0;
			int intTaskStatus = ( int ) QueuedTaskStatus.Processed;

			string countSql = @$"SELECT COUNT(1) AS total_cnt 
				FROM {mMapping.ResultsQueueTableName} 
				WHERE task_status = {intTaskStatus}";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				if ( await rdr.ReadAsync() )
					total = rdr.GetInt64( 0 );
			}

			return total;
		}
	}
}
