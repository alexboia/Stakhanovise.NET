using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardStandardTaskExecutorRegistrySetup : IStandardTaskExecutorRegistrySetup
	{
		private IDependencyResolver mResolver = null;
		
		public StandardDependencySetup mDependencySetup = 
			new StandardDependencySetup();

		public IStandardTaskExecutorRegistrySetup WithResolver ( IDependencyResolver resolver )
		{
			if ( resolver == null )
				throw new ArgumentNullException( nameof( resolver ) );

			mResolver = resolver;
			return this;
		}

		public IStandardTaskExecutorRegistrySetup SetupDependencies ( Action<IDependencySetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mDependencySetup );
			return this;
		}

		public IDependencyResolver GetDependencyResolver()
		{
			if ( mResolver == null )
				mResolver = new StandardDependencyResolver();

			mResolver.Load( mDependencySetup.DependencyRegistrations );
			return mResolver;
		}
	}
}
