using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardDependencyBindingScopeSetup : IDependencyBindingScopeSetup
	{
		private DependencyRegistration mDepRegistration;

		public StandardDependencyBindingScopeSetup ( DependencyRegistration depRegistration )
		{
			mDepRegistration = depRegistration
				?? throw new ArgumentNullException( nameof( depRegistration ) );
		}

		public void InSingletonScope ()
		{
			mDepRegistration.Scope = DependencyScope.Singleton;
		}

		public void InThreadScope ()
		{
			mDepRegistration.Scope = DependencyScope.Thread;
		}

		public void InTransientScope ()
		{
			mDepRegistration.Scope = DependencyScope.Transient;
		}
	}
}
