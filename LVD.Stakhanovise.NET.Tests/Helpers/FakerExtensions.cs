using System;
using System.Collections.Generic;
using System.Text;
using Bogus;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class FakerExtensions
	{
		public static T PickRandomWithout<T> ( this Faker faker, IEnumerable<T> from, T exclude )
		{
			T pick = faker.PickRandom<T>( from );

			while ( EqualityComparer<T>.Default.Equals( pick, exclude ) )
			{
				pick = faker.PickRandom<T>( from );
			}

			return pick;
		}
	}
}
