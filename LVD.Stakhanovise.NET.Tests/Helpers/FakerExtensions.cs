using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using LVD.Stakhanovise.NET.Model;

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

		public static AppMetric RandomAppMetric ( this Faker faker, AppMetricId id,
			long minValue = 0,
			long maxValue = long.MaxValue )
		{
			return new AppMetric( id, faker.Random.Long( minValue, maxValue ) );
		}

		public static List<AppMetric> RandomAppMetrics ( this Faker faker, IEnumerable<AppMetricId> forMetricIds,
			long minValue = 0,
			long maxValue = long.MaxValue )
		{
			List<AppMetric> metrics =
				new List<AppMetric>();

			foreach ( AppMetricId mId in forMetricIds )
			{
				AppMetric newMetric = faker.RandomAppMetric( mId, minValue, maxValue );
				metrics.Add( newMetric );
			}

			return metrics;
		}
	}
}
