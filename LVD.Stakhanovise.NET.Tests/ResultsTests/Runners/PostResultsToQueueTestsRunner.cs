using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Support;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.ResultsTests
{
	public class PostResultsToQueueTestsRunner
	{
		private readonly TaskQueueDataSource mDataSource;

		private readonly Func<TaskExecutionResult> mExecutionResultFactory;

		private List<IQueuedTaskResult> mResults = new List<IQueuedTaskResult>();

		public PostResultsToQueueTestsRunner( TaskQueueDataSource dataSource, 
			Func<TaskExecutionResult> executionResultFactory )
		{
			mDataSource = dataSource;
			mExecutionResultFactory = executionResultFactory;
		}

		public async Task RunTestsAsync( PostgreSqlTaskResultQueue resultQueue )
		{
			Reset();
			await resultQueue.StartAsync();

			foreach ( IQueuedTaskToken token in mDataSource.SeededTaskTokens )
			{
				if ( token.CanBeUpdated )
					token.UdpateFromExecutionResult( mExecutionResultFactory.Invoke() );

				await resultQueue.PostResultAsync( token.LastQueuedTaskResult );
				mResults.Add( token.LastQueuedTaskResult );
			}

			await resultQueue.StopAsync();
		}

		private void Reset()
		{
			mResults.Clear();
		}

		public List<IQueuedTaskResult> Results
			=> mResults;
	}
}
