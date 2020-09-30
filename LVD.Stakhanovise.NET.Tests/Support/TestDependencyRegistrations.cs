using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Tests.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class TestDependencyRegistrations
	{
		public static List<DependencyRegistration> GetAll()
		{
			return new List<DependencyRegistration>()
			{
				DependencyRegistration.BindToInstance( typeof( ISampleExecutorDependency ),
					new SampleExecutorDependencyImpl() )
			};
		}
	}
}
