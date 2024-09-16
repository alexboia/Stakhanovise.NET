using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.ModelTests
{
	[TestFixture]
	public class AsyncProcessingRequestBatchProcessorTests
	{
		private const int DefaultTimeoutMilliseconds = 1000;

		private const int MaxTimeoutMilliseconds = 100000;

		private const int DefaultMaxFailCount = 5;

		[Test]
		[Repeat( 10 )]
		public async Task Test_CanStartStop()
		{
			AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<int>> processor =
				new AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<int>>( NoOpProcessingDelegate,
				CreateLogger() );

			await processor.StartAsync();
			ClassicAssert.IsTrue( processor.IsRunning );

			await processor.StopAsync();
			ClassicAssert.IsFalse( processor.IsRunning );
		}

		private Task NoOpProcessingDelegate( AsyncProcessingRequestBatch<AsyncProcessingRequest<int>> requestBatch )
		{
			return Task.CompletedTask;
		}

		[Test]
		[TestCase( 1, true )]
		[TestCase( 5, true )]
		[TestCase( 100, true )]
		[TestCase( 1000, true )]
		[TestCase( 1, false )]
		[TestCase( 5, false )]
		[TestCase( 100, false )]
		[TestCase( 1000, false )]
		[Repeat( 10 )]
		public async Task Test_CanProcess_WhenNoErrors_AllRequestsProcessed( int nRequests, bool explicitWait )
		{
			List<long> processedRequestIds =
				new List<long>();
			List<AsyncProcessingRequest<long>> generatedRequests =
				new List<AsyncProcessingRequest<long>>();

			CountdownEvent countdowntHandle =
				new CountdownEvent( nRequests );

			AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<long>> processor =
				new AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<long>>( batch
					=> SetCompletedAndCollectBatchProcessingDelegate( batch,
						processedRequestIds,
						countdowntHandle ),
				CreateLogger() );

			await processor.StartAsync();

			generatedRequests = await GenerateAndPostRequestsAsync( processor,
				nRequests );

			if ( explicitWait )
				countdowntHandle.Wait();

			await processor.StopAsync();

			ClassicAssert.AreEqual( generatedRequests.Count,
				processedRequestIds.Count );

			foreach ( AsyncProcessingRequest<long> request in generatedRequests )
			{
				ClassicAssert.IsTrue( request.IsCompleted );
				ClassicAssert.IsFalse( request.IsTimedOut );
				ClassicAssert.IsFalse( request.IsFaulted );

				ClassicAssert.AreEqual( request.Id,
					request.Result );

				CollectionAssert.Contains( processedRequestIds,
					request.Id );
			}
		}

		private async Task<List<AsyncProcessingRequest<long>>> GenerateAndPostRequestsAsync( AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<long>> processor,
			int nRequests )
		{
			long lastRequestId = 0;
			List<AsyncProcessingRequest<long>> generatedRequests =
				new List<AsyncProcessingRequest<long>>();

			for ( int i = 0; i < nRequests; i++ )
			{
				long requestId = ++lastRequestId;

				AsyncProcessingRequest<long> request = new AsyncProcessingRequest<long>( requestId,
					DefaultTimeoutMilliseconds,
					DefaultMaxFailCount );

				generatedRequests.Add( request );
				await processor.PostRequestAsync( request );
			}

			return generatedRequests;
		}

		private Task SetCompletedAndCollectBatchProcessingDelegate( AsyncProcessingRequestBatch<AsyncProcessingRequest<long>> requestBatch,
			List<long> processedRequestIds,
			CountdownEvent countdowntHandle )
		{
			foreach ( AsyncProcessingRequest<long> request in requestBatch )
			{
				request.SetCompleted( request.Id );
				processedRequestIds.Add( request.Id );
				countdowntHandle.Signal();
			}

			return Task.CompletedTask;
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public async Task Test_CanHandleProcessingFailures_ExplicitWait_AllRetriedHaveBeenProcessed( int nRequests )
		{
			Dictionary<long, int> processedRequestIds =
				new Dictionary<long, int>();
			List<AsyncProcessingRequest<long>> generatedRequests =
				new List<AsyncProcessingRequest<long>>();

			CountdownEvent countdowntHandle =
				new CountdownEvent( nRequests );

			AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<long>> processor =
				new AsyncProcessingRequestBatchProcessor<AsyncProcessingRequest<long>>( batch
					=> SelectivelySetFailedAndCollectBatchProcessingDelegate( batch,
						processedRequestIds,
						countdowntHandle ),
				CreateLogger() );

			await processor.StartAsync();

			generatedRequests = await GenerateAndPostRequestsAsync( processor,
				nRequests );

			countdowntHandle.Wait();
			await processor.StopAsync();

			ClassicAssert.AreEqual( generatedRequests.Count,
				processedRequestIds.Count );

			foreach ( AsyncProcessingRequest<long> request in generatedRequests )
			{
				ClassicAssert.IsTrue( request.IsCompleted );
				ClassicAssert.IsFalse( request.IsTimedOut );

				if ( request.Id % 3 != 2 )
				{
					ClassicAssert.IsFalse( request.IsFaulted );
					ClassicAssert.AreEqual( request.Id,
						request.Result );
				}
				else
					ClassicAssert.IsTrue( request.IsFaulted );

				ClassicAssert.IsTrue( processedRequestIds
					.ContainsKey( request.Id ) );

				int expectedProcessingCount;
				int processingCount = processedRequestIds [ request.Id ];

				if ( request.Id % 3 == 0 )
					expectedProcessingCount = 1;
				else if ( request.Id % 3 == 1 )
					//Number of times failed + successful processing
					expectedProcessingCount = ( DefaultMaxFailCount - 1 ) + 1;
				else
					expectedProcessingCount = DefaultMaxFailCount;

				ClassicAssert.AreEqual( expectedProcessingCount,
					processingCount );
			}
		}

		private Task SelectivelySetFailedAndCollectBatchProcessingDelegate( AsyncProcessingRequestBatch<AsyncProcessingRequest<long>> requestBatch,
			Dictionary<long, int> processedRequestIds,
			CountdownEvent countdowntHandle )
		{
			foreach ( AsyncProcessingRequest<long> request in requestBatch )
			{
				if ( request.Id % 3 == 0 )
					request.SetCompleted( request.Id );
				else if ( request.Id % 3 == 1 )
				{
					if ( request.CurrentFailCount < DefaultMaxFailCount - 1 )
						request.SetFailed( new Exception( "Request failed" ) );
					else
						request.SetCompleted( request.Id );
				}
				else if ( request.Id % 3 == 2 )
					request.SetFailed( new Exception( "Request failed" ) );

				if ( !processedRequestIds.TryGetValue( request.Id, out int processingCount ) )
					processingCount = 0;

				processedRequestIds [ request.Id ] =
					processingCount + 1;

				if ( request.IsCompleted )
					countdowntHandle.Signal();
			}

			return Task.CompletedTask;
		}

		//Test 3 - Stop without waiting for all the requests to be processed

		private IStakhanoviseLogger CreateLogger()
		{
			return NoOpLogger.Instance;
		}
	}
}
