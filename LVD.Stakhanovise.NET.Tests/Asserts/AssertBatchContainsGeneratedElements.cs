using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
			ClassicAssert.NotNull( batch );
			ClassicAssert.AreEqual( mGeneratedElements.Count,
				batch.Count );

			foreach ( TElement bElement in batch )
				CollectionAssert.Contains( mGeneratedElements,
					bElement );
		}
	}
}
