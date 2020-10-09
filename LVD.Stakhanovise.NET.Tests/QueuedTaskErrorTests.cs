using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class QueuedTaskErrorTests
	{
		[Test]
		public void Test_CanCreateFromException ()
		{
			QueuedTaskError errBase = new QueuedTaskError( new Exception( "Sample exception message" ) );

			Assert.AreEqual( "System.Exception", errBase.Type );
			Assert.AreEqual( "Sample exception message", errBase.Message );
			Assert.IsNull( errBase.StackTrace );

			QueuedTaskError errInvOp = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception message" ) );

			Assert.AreEqual( "System.InvalidOperationException", errInvOp.Type );
			Assert.AreEqual( "Sample invalid operation exception message", errInvOp.Message );
			Assert.IsNull( errInvOp.StackTrace );

			QueuedTaskError errAppThrown;

			try
			{
				throw new ApplicationException( "Sample application exception message" );
			}
			catch ( Exception exc )
			{
				errAppThrown = new QueuedTaskError( exc );
			}

			Assert.AreEqual( "System.ApplicationException", errAppThrown.Type );
			Assert.AreEqual( "Sample application exception message", errAppThrown.Message );
			Assert.NotNull( errAppThrown.StackTrace );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanCompare_AreEqual ()
		{
			Faker faker =
				new Faker();

			QueuedTaskError err1 = new QueuedTaskError( faker.Random.AlphaNumeric( 10 ),
				faker.Lorem.Sentence(),
				faker.Random.String() );

			QueuedTaskError err2 = new QueuedTaskError( err1.Type,
				err1.Message,
				err1.StackTrace );

			Assert.AreEqual( err1, err1 );
			Assert.AreEqual( err1, err2 );
		}

		[Test]
		[Repeat( 10 )]
		public void Test_CanCompare_AreNotEqual ()
		{
			Faker faker =
				new Faker();

			QueuedTaskError err1 = new QueuedTaskError( faker.Random.AlphaNumeric( 10 ),
				faker.Lorem.Sentence(),
				faker.Random.String() );

			QueuedTaskError err2 = new QueuedTaskError( faker.Random.AlphaNumeric( 10 ),
				faker.Lorem.Sentence(),
				faker.Random.String() );

			Assert.AreNotEqual( err1, err2 );
		}
	}
}
