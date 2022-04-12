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
				GenericCounts counts = new GenericCounts()
				{
					TotalTasksInQueue =
						await ComputeTotalTasksInQueueAsync( conn ),
					TotalResultsInResultQueue =
						await ComputeTotalResultsInResultQueueAsync( conn ),
					TotalCompletedResultsInResultsQueue =
						await ComputeTotalCompletedResultsInResultQueueAsync( conn )
				};

				await conn.CloseAsync();
				return counts;
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

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
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

			string countSql = @$"SELECT COUNT(1) AS total_cnt 
				FROM {mMapping.ResultsQueueTableName} 
				WHERE task_status = {CompletedTaskStatusAsInt}";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
			{
				if ( await rdr.ReadAsync() )
					total = rdr.GetInt64( 0 );
			}

			return total;
		}

		public async Task<PayloadCounts> ComputePayloadCountsAsync()
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			{
				PayloadCounts counts = new PayloadCounts()
				{
					TotalTasksInQueuePerPayload =
						await ComputePerPayloadCountTasksInQueueAsync( conn ),
					TotalResultsInResultQueuePerPayload =
						await ComputePerPayloadCountResultsInResultQueueAsync( conn ),
					TotalCompletedResultsInResultsQueuePerPayload =
						await ComputePerPayloadCountCompletedResultsInResultQueueAsync( conn )
				};

				await conn.CloseAsync();
				return counts;
			}
		}

		private async Task<Dictionary<string, long>> ComputePerPayloadCountTasksInQueueAsync( NpgsqlConnection conn )
		{
			string countSql = @$"SELECT task_type, COUNT(1) AS total_cnt 
				FROM {mMapping.QueueTableName}
				GROUP BY task_type";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				return await ReadPayloadCountsFromDbReader( rdr );
		}

		private async Task<Dictionary<string, long>> ReadPayloadCountsFromDbReader( NpgsqlDataReader rdr )
		{
			Dictionary<string, long> counts =
				new Dictionary<string, long>();

			while ( await rdr.ReadAsync() )
			{
				string type = rdr.GetString( 0 );
				long count = rdr.GetInt64( 1 );
				counts[ type ] = count;
			}

			return counts;
		}

		private async Task<Dictionary<string, long>> ComputePerPayloadCountResultsInResultQueueAsync( NpgsqlConnection conn )
		{
			string countSql = @$"SELECT task_type, COUNT(1) AS total_cnt 
				FROM {mMapping.ResultsQueueTableName}
				GROUP BY task_type";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				return await ReadPayloadCountsFromDbReader( rdr );
		}

		private async Task<Dictionary<string, long>> ComputePerPayloadCountCompletedResultsInResultQueueAsync( NpgsqlConnection conn )
		{
			string countSql = @$"SELECT task_type, COUNT(1) AS total_cnt 
				FROM {mMapping.ResultsQueueTableName}
				WHERE task_status = {CompletedTaskStatusAsInt}
				GROUP BY task_type";

			using ( NpgsqlCommand cmd = new NpgsqlCommand( countSql, conn ) )
			using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				return await ReadPayloadCountsFromDbReader( rdr );
		}

		public async Task<IEnumerable<OwnerProcessCounts>> ComputeExecutionPerformanceStatsOwnerProcessCountsAsync()
		{
			using ( NpgsqlConnection conn = await OpenConnectionAsync() )
			{
				List<OwnerProcessCounts> countsList =
					new List<OwnerProcessCounts>();

				string countsSql = @$"SELECT et_owner_process_id, COUNT(et_owner_process_id) as total_cnt
					FROM {mMapping.ExecutionTimeStatsTableName}
					GROUP BY et_owner_process_id";

				using ( NpgsqlCommand cmd = new NpgsqlCommand( countsSql, conn ) )
				using ( NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync() )
				{
					while ( await rdr.ReadAsync() )
					{
						OwnerProcessCounts countsItem = new OwnerProcessCounts()
						{
							OwnerProcessId = rdr.GetString( 0 ),
							Count = rdr.GetInt64( 1 )
						};
					}
				}

				await conn.CloseAsync();
				return countsList;
			}
		}

		private int CompletedTaskStatusAsInt
			=> ( int ) QueuedTaskStatus.Processed;
	}
}
