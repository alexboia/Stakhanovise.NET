using Bogus;
using LVD.Stakhanovise.NET.Common.Tests.TestDataStructures;
using NUnit.Framework;
using System;
using System.Linq;

namespace LVD.Stakhanovise.NET.Common.Tests
{
	[TestFixture]
	public class SupportedValuesContainerTests
	{
		[Test]
		public void Test_TryCreate_NullKeySelector_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>( () => new SupportedValuesContainer<SamplePerson, string>( null ) );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CreatedWithType_ThatHasNoPredefinedInstances()
		{
			Faker faker = new Faker();
			SupportedValuesContainer<SamplePerson, string> supportedPersons =
				new SupportedValuesContainer<SamplePerson, string>( p => p.Name );

			Assert.NotNull( supportedPersons.SupportedValues );
			Assert.IsEmpty( supportedPersons.SupportedValues );

			string personName = faker.Name.FullName();

			Assert.IsFalse( supportedPersons.IsSupported( personName ) );
			Assert.IsNull( supportedPersons.TryParse( personName ) );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CreatedWithType_ThatHasPredefinedInstances()
		{
			SupportedValuesContainer<SamplePredefinedPerson, Guid> supportedPersons =
				new SupportedValuesContainer<SamplePredefinedPerson, Guid>( p => p.Id );

			Assert.NotNull( supportedPersons.SupportedValues );
			Assert.AreEqual( 3, supportedPersons.SupportedValues.Count() );

			Assert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonA.Id ) );
			Assert.AreSame( SamplePredefinedPerson.PersonA, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonA.Id ) );

			Assert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonB.Id ) );
			Assert.AreSame( SamplePredefinedPerson.PersonB, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonB.Id ) );

			Assert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonC.Id ) );
			Assert.AreSame( SamplePredefinedPerson.PersonC, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonC.Id ) );

			Guid randomPersonId = Guid.NewGuid();
			Assert.IsFalse( supportedPersons.IsSupported( randomPersonId ) );
			Assert.IsNull( supportedPersons.TryParse( randomPersonId ) );
		}
	}
}
