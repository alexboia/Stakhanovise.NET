using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors
{
	public interface IDependencyProvider
	{
		object CreateInstance ( IDependencyResolver resolver );

		Type Type { get; }
	}
}
