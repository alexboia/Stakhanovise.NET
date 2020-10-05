using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class AsSingletonSampleDependencyProvider : IDependencyProvider<IAsSingletonSampleDependency>
	{
		public Type Type => typeof( IAsSingletonSampleDependency );

		public object CreateInstance ( IDependencyResolver resolver )
		{
			return new AsSingletonSampleDependencyImpl();
		}
	}
}
