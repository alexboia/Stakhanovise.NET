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
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class QueuedTaskErrorTests
	{
		[Test]
		public void Test_CanCreateFromException ()
		{
			QueuedTaskError errBase = new QueuedTaskError( new Exception( "Sample exception message" ) );

			ClassicAssert.AreEqual( "System.Exception", errBase.Type );
			ClassicAssert.AreEqual( "Sample exception message", errBase.Message );
			ClassicAssert.IsNull( errBase.StackTrace );

			QueuedTaskError errInvOp = new QueuedTaskError( new InvalidOperationException( "Sample invalid operation exception message" ) );

			ClassicAssert.AreEqual( "System.InvalidOperationException", errInvOp.Type );
			ClassicAssert.AreEqual( "Sample invalid operation exception message", errInvOp.Message );
			ClassicAssert.IsNull( errInvOp.StackTrace );

			QueuedTaskError errAppThrown;

			try
			{
				throw new ApplicationException( "Sample application exception message" );
			}
			catch ( Exception exc )
			{
				errAppThrown = new QueuedTaskError( exc );
			}

			ClassicAssert.AreEqual( "System.ApplicationException", errAppThrown.Type );
			ClassicAssert.AreEqual( "Sample application exception message", errAppThrown.Message );
			ClassicAssert.NotNull( errAppThrown.StackTrace );
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

			ClassicAssert.AreEqual( err1, err1 );
			ClassicAssert.AreEqual( err1, err2 );
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

			ClassicAssert.AreNotEqual( err1, err2 );
		}
	}
}
