using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class CollectionsExtensions
	{
		public static List<T>[] MultiplyCollection<T> ( this IEnumerable<T> source, int count )
		{
			List<T>[] result = new List<T>[ count ];

			for ( int i = 0; i < count; i++ )
				result[ i ] = new List<T>( source );

			return result;
		}
	}
}
