using Bogus;
using LVD.Stakhanovise.NET.Common.Tests.TestDataStructures;
using LVD.Stakhanovise.NET.Helpers;
using NUnit.Framework;

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

			Assert.IsNull( json );
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
			Assert.IsNotNull( json );
			Assert.IsNotEmpty( json );

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
			Assert.IsNotNull( json );
			Assert.IsNotEmpty( json );

			SamplePerson deserializedSamplePerson = json.AsObjectFromJson() as SamplePerson;
			Assert.IsNotNull( deserializedSamplePerson );

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
			Assert.AreEqual( expected.Name,
				actual.Name );
			Assert.AreEqual( expected.Job,
				actual.Job );
			Assert.AreEqual( expected.Age,
				actual.Age );
			Assert.AreEqual( expected.Bio,
				actual.Bio );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CanSerializeDeserialize_WithoutTypeInformation_TypedAPI()
		{
			SamplePerson samplePerson = CreateSamplePerson();

			string json = samplePerson.ToJson( includeTypeInformation: false );
			Assert.IsNotNull( json );
			Assert.IsNotEmpty( json );

			SamplePerson deserializedSamplePerson = json.AsObjectFromJson<SamplePerson>();
			Assert.IsNotNull( deserializedSamplePerson );

			AssertSamplePersonInstancesEqual( samplePerson,
				deserializedSamplePerson );
		}
	}
}
