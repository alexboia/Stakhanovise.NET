using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET.Common.Tests
{
	[TestFixture]
	public class QueuedTaskTests
	{
		[Test]
		public void Test_CanCompare_AreEqual_BothWithIds()
		{
			Guid id = Guid.NewGuid();

			QueuedTask t1 = new QueuedTask( id );
			QueuedTask t2 = new QueuedTask( id );

			Assert.AreEqual( t1, t2 );
			Assert.IsTrue( t1.Equals( t2 ) );
		}

		[Test]
		public void Test_CanCompare_AreEqual_BothWithoutIds()
		{
			Guid id = Guid.NewGuid();

			QueuedTask t1 = new QueuedTask( id );
			QueuedTask t2 = t1;

			Assert.AreEqual( t1, t2 );
			Assert.IsTrue( t1.Equals( t2 ) );
		}

		[Test]
		public void Test_CanCompare_AreNotEqual_BothWithIds()
		{
			QueuedTask t1 = new QueuedTask( Guid.NewGuid() );
			QueuedTask t2 = new QueuedTask( Guid.NewGuid() );

			Assert.AreNotEqual( t1, t2 );
			Assert.IsFalse( t1.Equals( t2 ) );
		}

		[Test]
		public void Test_CanCompare_AreNotEqual_BothWithoutIds()
		{
			QueuedTask t1 = new QueuedTask();
			QueuedTask t2 = new QueuedTask();

			Assert.AreNotEqual( t1, t2 );
			Assert.IsFalse( t1.Equals( t2 ) );
		}
	}
}
