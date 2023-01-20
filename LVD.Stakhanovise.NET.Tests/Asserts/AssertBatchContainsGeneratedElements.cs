using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertBatchContainsGeneratedElements<TElement>
	{
		private List<TElement> mGeneratedElements;

		private AssertBatchContainsGeneratedElements( List<TElement> generatedElements )
		{
			mGeneratedElements = generatedElements
				?? throw new ArgumentNullException( nameof( generatedElements ) );
		}

		public static AssertBatchContainsGeneratedElements<TElement> For( List<TElement> generatedElements )
		{
			return new AssertBatchContainsGeneratedElements<TElement>( generatedElements );
		}

		public void Check( AsyncProcessingRequestBatch<TElement> batch )
		{
			Assert.NotNull( batch );
			Assert.AreEqual( mGeneratedElements.Count,
				batch.Count );

			foreach ( TElement bElement in batch )
				CollectionAssert.Contains( mGeneratedElements,
					bElement );
		}
	}
}
