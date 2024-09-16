using Bogus;
using LVD.Stakhanovise.NET.Common.Tests.TestDataStructures;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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

			ClassicAssert.NotNull( supportedPersons.SupportedValues );
			ClassicAssert.IsEmpty( supportedPersons.SupportedValues );

			string personName = faker.Name.FullName();

			ClassicAssert.IsFalse( supportedPersons.IsSupported( personName ) );
			ClassicAssert.IsNull( supportedPersons.TryParse( personName ) );
		}

		[Test]
		[Repeat( 25 )]
		public void Test_CreatedWithType_ThatHasPredefinedInstances()
		{
			SupportedValuesContainer<SamplePredefinedPerson, Guid> supportedPersons =
				new SupportedValuesContainer<SamplePredefinedPerson, Guid>( p => p.Id );

			ClassicAssert.NotNull( supportedPersons.SupportedValues );
			ClassicAssert.AreEqual( 3, supportedPersons.SupportedValues.Count() );

			ClassicAssert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonA.Id ) );
			ClassicAssert.AreSame( SamplePredefinedPerson.PersonA, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonA.Id ) );

			ClassicAssert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonB.Id ) );
			ClassicAssert.AreSame( SamplePredefinedPerson.PersonB, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonB.Id ) );

			ClassicAssert.IsTrue( supportedPersons.IsSupported( SamplePredefinedPerson.PersonC.Id ) );
			ClassicAssert.AreSame( SamplePredefinedPerson.PersonC, supportedPersons
				.TryParse( SamplePredefinedPerson.PersonC.Id ) );

			Guid randomPersonId = Guid.NewGuid();
			ClassicAssert.IsFalse( supportedPersons.IsSupported( randomPersonId ) );
			ClassicAssert.IsNull( supportedPersons.TryParse( randomPersonId ) );
		}
	}
}
