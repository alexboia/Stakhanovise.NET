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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests.AppMetricsTests
{
	[TestFixture]
	public class AppMetricTests
	{
		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanUpdate_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];

			List<long> threadValues =
				new List<long>( nThreads );

			ConcurrentBag<long> prevValues =
				new ConcurrentBag<long>();

			long initialValue = faker.Random
				.Long();

			for ( int i = 0; i < nThreads; i++ )
				threadValues.Add( faker.Random.Long() );

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				initialValue );

			for ( int i = 0; i < nThreads; i++ )
			{
				long threadValue = threadValues[ i ];

				Thread setterThread = new Thread( ()
					=> prevValues.Add( metric.Update( threadValue ) ) );

				threads[ i ] = setterThread;
				setterThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			prevValues.Add( metric.Update( faker.Random.Long() ) );

			Assert.AreEqual( threadValues.Count + 1,
				prevValues.Count );

			CollectionAssert.Contains( prevValues, initialValue );
			foreach ( long threadVal in threadValues )
				CollectionAssert.Contains( prevValues, threadVal );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanIncrement_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				0 );

			for ( int i = 0; i < nThreads; i++ )
			{
				Thread addThread = new Thread( () => metric.Increment() );
				threads[ i ] = addThread;
				addThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			Assert.AreEqual( nThreads, metric.Value );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanDecrement_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				nThreads );

			for ( int i = 0; i < nThreads; i++ )
			{
				Thread addThread = new Thread( () => metric.Decrement() );
				threads[ i ] = addThread;
				addThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			Assert.AreEqual( 0, metric.Value );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanAdd_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];
			long valueToAdd = faker.Random.Long( 1, 10 );

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				0 );

			for ( int i = 0; i < nThreads; i++ )
			{
				Thread addThread = new Thread( () => metric.Add( valueToAdd ) );
				threads[ i ] = addThread;
				addThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			Assert.AreEqual( nThreads * valueToAdd, metric.Value );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanSetMin_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];

			List<long> threadValues =
				new List<long>( nThreads );

			long initialValue = faker.Random
				.Long( 100, 10000 );

			for ( int i = 0; i < nThreads; i++ )
				threadValues.Add( initialValue - i - 1 );

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				long.MaxValue );

			for ( int i = 0; i < nThreads; i++ )
			{
				long threadValue = threadValues[ i ];

				Thread minThread = new Thread( ()
					=> metric.Min( threadValue ) );

				threads[ i ] = minThread;
				minThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			Assert.AreEqual( threadValues.Min(),
				metric.Value );
		}

		[Test]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 10 )]
		public void Test_CanSetMax_Parallel ( int nThreads )
		{
			Faker faker = new Faker();
			Thread[] threads = new Thread[ nThreads ];

			List<long> threadValues =
				new List<long>( nThreads );

			long initialValue = faker.Random
				.Long( 100, 10000 );

			for ( int i = 0; i < nThreads; i++ )
				threadValues.Add( initialValue + i + 1 );

			AppMetric metric = new AppMetric( faker.PickRandom( AppMetricId.BuiltInAppMetricIds ),
				long.MinValue );

			for ( int i = 0; i < nThreads; i++ )
			{
				long threadValue = threadValues[ i ];

				Thread maxThread = new Thread( ()
					=> metric.Max( threadValue ) );

				threads[ i ] = maxThread;
				maxThread.Start();
			}

			foreach ( Thread t in threads )
				t.Join();

			Assert.AreEqual( threadValues.Max(),
				metric.Value );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanJoinWith ()
		{
			Faker faker = new Faker();
			long value1 = faker.Random.Long( 1, 1000 );
			long value2 = faker.Random.Long( 1, 1000 );

			AppMetricId metricId = faker.PickRandom( AppMetricId.BuiltInAppMetricIds );

			AppMetric metric1 = new AppMetric( metricId,
				value1 );
			AppMetric metric2 = new AppMetric( metricId,
				value2 );

			AppMetric metric = metric1.JoinWith( metric2 );

			Assert.AreEqual( value1 + value2, metric.Value );
			Assert.AreNotSame( metric, metric1 );
			Assert.AreNotSame( metric, metric2 );
		}
	}
}

