using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Bogus;

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

			Assert.AreEqual( nElements,
				batch.Count );

			foreach ( int bElement in batch )
				CollectionAssert.Contains( generatedElements,
					bElement );
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

			Assert.AreEqual( 1,
				batch.Count );

			foreach ( int bElement in batch )
				CollectionAssert.Contains( generatedElements,
					bElement );
		}

		//Test 1 - Can be filled from request source - with cancellation

		[Test]
		[TestCase( 1 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 20 )]
		public void Test_CanBeFilled_WhenCancellationOccurs( int nElements )
		{
			BlockingCollection<int> source =
				new BlockingCollection<int>();
			List<int> generatedElements =
				new List<int>();
			CancellationTokenSource cancellationTokenSource =
				new CancellationTokenSource();

			AsyncProcessingRequestBatch<int> batch =
				new AsyncProcessingRequestBatch<int>( nElements );

			CancellationToken cancellationToken = cancellationTokenSource
				.Token;

			//TODO: requires further work
			cancellationToken.Register( () =>
			{
				for ( int i = 0; i < nElements; i++ )
				{
					generatedElements.Add( i );
					source.Add( i );
				}
			} );

			cancellationTokenSource.CancelAfter( 150 );
			batch.FillFrom( source, cancellationToken );

			Assert.AreEqual( nElements,
				batch.Count );

			foreach ( int bElement in batch )
				CollectionAssert.Contains( generatedElements,
					bElement );
		}
	}
}
