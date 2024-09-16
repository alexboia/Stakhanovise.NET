// 
// BSD 3-Clause License
// 
// Copyright (c) 2020 - 2023, Boia Alexandru
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
using Bogus;
using LVD.Stakhanovise.NET.Common.Tests.TestDataStructures;
using LVD.Stakhanovise.NET.Helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace LVD.Stakhanovise.NET.Common.Tests.HelpersTests
{
	[TestFixture]
	public class SerializationExtensionsTests
	{
		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_ToJson_Simple_WhenGivenNullObject_ReturnsNull( bool includeTypeInformation )
		{
			object obj = null;
			string json = obj.ToJson( includeTypeInformation );

			ClassicAssert.IsNull( json );
		}

		[Test]
		[TestCase( true )]
		[TestCase( false )]
		public void Test_ToJson_Simple_WhenGivenNonNullAnonymousObject_ReturnsProperJson( bool includeTypeInformation )
		{
			object obj = new
			{
				Name = "John",
				Age = 30,
				Job = "Central Committee Member"
			};

			string json = obj.ToJson( includeTypeInformation );
			ClassicAssert.IsNotNull( json );
			ClassicAssert.IsNotEmpty( json );

			Assert.That( json, Contains.Substring( "Name" ) );
			Assert.That( json, Contains.Substring( "John" ) );

			Assert.That( json, Contains.Substring( "Age" ) );
			Assert.That( json, Contains.Substring( "30" ) );

			Assert.That( json, Contains.Substring( "Job" ) );
			Assert.That( json, Contains.Substring( "Central Committee Member" ) );

			if ( includeTypeInformation )
				Assert.That( json, Contains.Substring( "$type" ) );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CanSerializeDeserialize_WithTypeInformation_UntypedAPI()
		{
			SamplePerson samplePerson = CreateSamplePerson();

			string json = samplePerson.ToJson( includeTypeInformation: true );
			ClassicAssert.IsNotNull( json );
			ClassicAssert.IsNotEmpty( json );

			SamplePerson deserializedSamplePerson = json.AsObjectFromJson() as SamplePerson;
			ClassicAssert.IsNotNull( deserializedSamplePerson );

			AssertSamplePersonInstancesEqual( samplePerson,
				deserializedSamplePerson );
		}

		private SamplePerson CreateSamplePerson()
		{
			Faker faker = new Faker();
			SamplePerson samplePerson = new SamplePerson();
			samplePerson.Name = faker.Name.FullName();
			samplePerson.Job = faker.Lorem.Paragraph();
			samplePerson.Age = faker.Random.Int( 10, 100 );
			samplePerson.Bio = faker.Random.Int() % 2 == 0 ? faker.Lorem.Paragraph() : null;
			return samplePerson;
		}

		private void AssertSamplePersonInstancesEqual( SamplePerson expected,
			SamplePerson actual )
		{
			ClassicAssert.AreEqual( expected.Name,
				actual.Name );
			ClassicAssert.AreEqual( expected.Job,
				actual.Job );
			ClassicAssert.AreEqual( expected.Age,
				actual.Age );
			ClassicAssert.AreEqual( expected.Bio,
				actual.Bio );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CanSerializeDeserialize_WithoutTypeInformation_TypedAPI()
		{
			SamplePerson samplePerson = CreateSamplePerson();

			string json = samplePerson.ToJson( includeTypeInformation: false );
			ClassicAssert.IsNotNull( json );
			ClassicAssert.IsNotEmpty( json );

			SamplePerson deserializedSamplePerson = json.AsObjectFromJson<SamplePerson>();
			ClassicAssert.IsNotNull( deserializedSamplePerson );

			AssertSamplePersonInstancesEqual( samplePerson,
				deserializedSamplePerson );
		}
	}
}
