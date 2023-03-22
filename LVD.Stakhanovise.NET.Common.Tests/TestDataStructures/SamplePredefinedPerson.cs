using System;

namespace LVD.Stakhanovise.NET.Common.Tests.TestDataStructures
{
	public class SamplePredefinedPerson
	{
		public static readonly SamplePredefinedPerson PersonA =
			new SamplePredefinedPerson( Guid.NewGuid(), 
				new SamplePerson()
				{
					Name = "Person A",
					Age = 11,
					Bio = "Whatever Bio Person A"
				} 
			);

		public static readonly SamplePredefinedPerson PersonB =
			new SamplePredefinedPerson( Guid.NewGuid(), 
				new SamplePerson()
				{
					Name = "Person B",
					Age = 21

				} 
			);

		public static readonly SamplePredefinedPerson PersonC =
			new SamplePredefinedPerson( Guid.NewGuid(), 
				new SamplePerson()
				{
					Name = "Person C",
					Age = 91,
					Job = "Coal Miner"
				} 
			);

		private SamplePredefinedPerson( Guid id, SamplePerson data )
		{
			Id = id;
			Data = data;
		}

		public Guid Id
		{
			get; private set;
		}

		public SamplePerson Data
		{
			get; private set;
		}
	}
}
