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
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class TaskExecutionStatsTests
	{
		[Test]
		[Repeat( 10 )]
		public void Test_CanInitialize ()
		{
			Faker faker = new Faker();
			long sampleTime = faker.Random.Long( 1 );

			TaskExecutionStats stats = TaskExecutionStats.Initial( sampleTime );

			Assert.NotNull( stats );
			Assert.AreEqual( sampleTime, stats.AverageExecutionTime );
			Assert.AreEqual( sampleTime, stats.LastExecutionTime );
			Assert.AreEqual( sampleTime, stats.LongestExecutionTime );
			Assert.AreEqual( sampleTime, stats.FastestExecutionTime );
			Assert.AreEqual( sampleTime, stats.TotalExecutionTime );
			Assert.AreEqual( 1, stats.NumberOfExecutionCycles );
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[TestCase( 100 )]
		[Repeat( 10 )]
		public void Test_CanUpdateWithNewCycleExecutionTime ( int nCycles )
		{
			Faker faker = new Faker();
			long sampleTime = faker.Random.Long( 1 );
			long minSampleTime = sampleTime;
			long maxSampleTime = sampleTime;
			long totalSampleTime = sampleTime;

			TaskExecutionStats stats = TaskExecutionStats
				.Initial( sampleTime );

			Assert.NotNull( stats );

			for ( int i = 1; i <= nCycles; i++ )
			{
				long newSampleTime = faker.Random.Long( 1 );
				TaskExecutionStats newStats = stats.UpdateWithNewCycleExecutionTime( newSampleTime );

				totalSampleTime += newSampleTime;

				Assert.NotNull( newStats );
				Assert.AreNotSame( stats, newStats );

				Assert.AreEqual( newSampleTime, newStats.LastExecutionTime );
				Assert.AreEqual( i + 1, newStats.NumberOfExecutionCycles );

				Assert.AreEqual( Math.Max( maxSampleTime, newSampleTime ), newStats.LongestExecutionTime );
				Assert.AreEqual( Math.Min( minSampleTime, newSampleTime ), newStats.FastestExecutionTime );
				Assert.AreEqual( totalSampleTime, newStats.TotalExecutionTime );
				Assert.AreEqual( ( long )Math.Ceiling( ( double )totalSampleTime / ( i + 1 ) ), newStats.AverageExecutionTime );

				minSampleTime = newStats.FastestExecutionTime;
				maxSampleTime = newStats.LongestExecutionTime;
				stats = newStats;
			}
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanCopy ()
		{
			Faker faker = new Faker();
			TaskExecutionStats stats = new TaskExecutionStats( lastExecutionTime: faker.Random.Long( 1 ),
				averageExecutionTime: faker.Random.Long( 1 ),
				fastestExecutionTime: faker.Random.Long( 1 ),
				longestExecutionTime: faker.Random.Long( 1 ),
				totalExecutionTime: faker.Random.Long( 1 ),
				numberOfExecutionCycles: faker.Random.Long( 1, 10 ) );

			TaskExecutionStats statsCopy = stats.Copy();

			Assert.NotNull( statsCopy );
			Assert.AreEqual( statsCopy, stats );
			Assert.AreNotSame( statsCopy, stats );
		}

		[Test]
		public void Test_CanCreateZeroStats ()
		{
			TaskExecutionStats stats = TaskExecutionStats.Zero();

			Assert.NotNull( stats );
			Assert.AreEqual( 0, stats.AverageExecutionTime );
			Assert.AreEqual( 0, stats.LongestExecutionTime );
			Assert.AreEqual( 0, stats.FastestExecutionTime );
			Assert.AreEqual( 0, stats.LastExecutionTime );
			Assert.AreEqual( 0, stats.NumberOfExecutionCycles );
			Assert.AreEqual( 0, stats.TotalExecutionTime );

			Assert.AreNotSame( TaskExecutionStats.Zero(), TaskExecutionStats.Zero() );
			Assert.AreEqual( TaskExecutionStats.Zero(), TaskExecutionStats.Zero() );
		}

		[Test]
		public void Test_CanComputeDifference_WithChanges_BetweenFixedValues ()
		{
			TaskExecutionStats stats1 = new TaskExecutionStats( lastExecutionTime: 100, 
				averageExecutionTime: 75, 
				fastestExecutionTime: 50, 
				longestExecutionTime: 100, 
				totalExecutionTime: 150, 
				numberOfExecutionCycles: 2 );

			TaskExecutionStats stats2 = new TaskExecutionStats( lastExecutionTime: 150,
				averageExecutionTime: 100,
				fastestExecutionTime: 50,
				longestExecutionTime: 150,
				totalExecutionTime: 300,
				numberOfExecutionCycles: 3 );

			TaskExecutionStats diff = stats2.Since( stats1 );

			Assert.NotNull( diff );
			Assert.AreEqual( 1, diff.NumberOfExecutionCycles );
			Assert.AreEqual( 50, diff.FastestExecutionTime );
			Assert.AreEqual( 150, diff.LongestExecutionTime );
			Assert.AreEqual( 150, diff.AverageExecutionTime );
			Assert.AreEqual( 150, diff.TotalExecutionTime );
			Assert.AreEqual( 150, diff.LastExecutionTime );
		}

		[Test]
		public void Test_CanComputeDifference_NoChanges_BetweenFixedValues ()
		{
			TaskExecutionStats stats1 = new TaskExecutionStats( lastExecutionTime: 100,
				averageExecutionTime: 75,
				fastestExecutionTime: 50,
				longestExecutionTime: 100,
				totalExecutionTime: 150,
				numberOfExecutionCycles: 2 );

			TaskExecutionStats stats2 = stats1.Copy();

			TaskExecutionStats diff = stats2.Since( stats1 );

			Assert.AreEqual( 0, diff.NumberOfExecutionCycles );
			Assert.AreEqual( 50, diff.FastestExecutionTime );
			Assert.AreEqual( 100, diff.LongestExecutionTime );
			Assert.AreEqual( 0, diff.AverageExecutionTime );
			Assert.AreEqual( 0, diff.TotalExecutionTime );
			Assert.AreEqual( 100, diff.LastExecutionTime );
		}
	}
}
