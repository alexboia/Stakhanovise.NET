// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Tests.Payloads;

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

		public static List<TaskPerformanceStats> RandomExecutionPerformanceStats ( this Faker faker, int count )
		{
			List<TaskPerformanceStats> stats =
				new List<TaskPerformanceStats>();

			for ( int i = 0; i < count; i++ )
			{
				long time = faker.Random.Long( 1, 1000000 );
				string payloadType = faker.Random.AlphaNumeric( 250 );

				stats.Add( new TaskPerformanceStats(
					payloadType,
					time ) );
			}

			return stats;
		}

		public static List<TaskPerformanceStats> RandomAllZeroExecutionPerformanceStats ( this Faker faker, int count )
		{
			List<TaskPerformanceStats> stats =
				new List<TaskPerformanceStats>();

			for ( int i = 0; i < count; i++ )
			{
				string payloadType = faker.Random.AlphaNumeric( 250 );

				stats.Add( new TaskPerformanceStats(
					payloadType,
					0 ) );
			}

			return stats;
		}

		public static List<TaskPerformanceStats> RandomExecutionPerformanceStats ( this Faker faker, IEnumerable<string> payloadTypes )
		{
			List<TaskPerformanceStats> stats =
				new List<TaskPerformanceStats>();

			foreach ( string payloadType in payloadTypes )
			{
				long time = faker.Random.Long( 1, 1000000 );

				stats.Add( new TaskPerformanceStats(
					payloadType,
					time ) );
			}

			return stats;
		}

		public static List<TaskPerformanceStats> RandomAllZeroExecutionPerformanceStats ( this Faker faker, IEnumerable<string> payloadTypes )
		{
			List<TaskPerformanceStats> stats =
				new List<TaskPerformanceStats>();

			foreach ( string payloadType in payloadTypes )
			{
				stats.Add( new TaskPerformanceStats(
					payloadType,
					0 ) );
			}

			return stats;
		}
	}
}
