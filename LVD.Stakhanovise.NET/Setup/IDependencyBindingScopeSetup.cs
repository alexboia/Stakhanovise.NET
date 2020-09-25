using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IDependencyBindingScopeSetup
	{
		void InSingletonScope ();

		void InThreadScope ();

		void InTransientScope ();
	}
}
