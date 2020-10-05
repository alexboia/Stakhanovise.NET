using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Executors
{
	public class AsTransientSampleDependencyProvider : IDependencyProvider<IAsTransientSampleDependency>
	{
		public Type Type => typeof( IAsTransientSampleDependency );

		public object CreateInstance ( IDependencyResolver resolver )
		{
			return new AsTransientSampleDependencyImpl();
		}
	}
}
