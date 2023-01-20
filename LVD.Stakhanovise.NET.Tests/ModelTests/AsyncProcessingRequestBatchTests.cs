using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.ModelTests
{
	[TestFixture]
	public class AsyncProcessingRequestBatchTests
	{
		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[TestCase( 1000 )]
		[Repeat( 10 )]
		public void Test_CanBeFilled_WhenNoCancellationOccurs_AlreadyHasElements_DoesNotExceedMaxSize( int nElements )
		{
			BlockingCollection<int> source =
				new BlockingCollection<int>();
			List<int> generatedElements =
				new List<int>();

			AsyncProcessingRequestBatch<int> batch =
				new AsyncProcessingRequestBatch<int>( nElements );

			for ( int i = 0; i < nElements; i++ )
			{
				generatedElements.Add( i );
				source.Add( i );
			}

			batch.FillFrom( source,
				CancellationToken.None );

			AssertBatchContainsGeneratedElements( generatedElements,
				batch );
		}

		private void AssertBatchContainsGeneratedElements( List<int> generatedElements, 
			AsyncProcessingRequestBatch<int> batch )
		{
			AssertBatchContainsGeneratedElements<int>
				.For( generatedElements )
				.Check( batch );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanBeFilled_WhenNoCancellationOccurs_NoElementsInBatch( int batchSize )
		{
			Faker faker =
				new Faker();
			BlockingCollection<int> source =
				new BlockingCollection<int>();
			List<int> generatedElements =
				new List<int>();

			AsyncProcessingRequestBatch<int> batch =
				new AsyncProcessingRequestBatch<int>( batchSize );

			Task generationTask = Task.Run( () =>
			 {
				 Task.Delay( 150 ).Wait();

				 int value = faker.Random.Int();
				 generatedElements.Add( value );
				 source.Add( value );
			 } );

			batch.FillFrom( source, CancellationToken.None );
			generationTask.Wait();

			AssertBatchContainsGeneratedElements( generatedElements,
				batch );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 20 )]
		public void Test_CanBeFilled_WhenCancellationOccurs_ElementsArePresentInSourceBeforeCancellation( int nElements )
		{
			BlockingCollection<int> source =
				new BlockingCollection<int>();
			List<int> generatedElements =
				new List<int>();
			CancellationTokenSource cancellationTokenSource =
				new CancellationTokenSource();

			AsyncProcessingRequestBatch<int> batch =
				new AsyncProcessingRequestBatch<int>( nElements );

			for ( int i = 0; i < nElements; i++ )
			{
				generatedElements.Add( i );
				source.Add( i );
			}

			cancellationTokenSource.Cancel();
			batch.FillFrom( source, cancellationTokenSource.Token );

			AssertBatchContainsGeneratedElements( generatedElements,
				batch );
		}
	}
}
