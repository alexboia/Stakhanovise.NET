using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class AsThreadSingletonSampleDependencyProvider : IDependencyProvider<IAsThreadSingletonSampleDependency>
	{
		public Type Type => typeof( IAsThreadSingletonSampleDependency );

		public object CreateInstance ( IDependencyResolver resolver )
		{
			return new AsThreadSingletonSampleDependencyImpl();
		}
	}
}
