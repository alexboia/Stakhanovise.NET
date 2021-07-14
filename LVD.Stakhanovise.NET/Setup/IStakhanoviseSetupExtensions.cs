using LVD.Stakhanovise.NET.Executors;
using System;

namespace LVD.Stakhanovise.NET.Setup
{
	public static class IStakhanoviseSetupExtensions
	{
		public static IStakhanoviseSetup SetupBuiltInTaskExecutorRegistryDependencies( this IStakhanoviseSetup stakhanoviseSetup,
			Action<IDependencySetup> setupAction )
		{
			if ( stakhanoviseSetup == null )
				throw new ArgumentNullException( nameof( stakhanoviseSetup ) );

			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			return stakhanoviseSetup.SetupTaskExecutorRegistry( execRegSetup =>
				execRegSetup.SetupBuiltInTaskExecutorRegistry( builtInExecRegSetup =>
					builtInExecRegSetup.SetupDependencies( setupAction )
				)
			);
		}

		public static IStakhanoviseSetup WithBuiltInTaskExecutorRegistryDependencyResolver( this IStakhanoviseSetup stakhanoviseSetup,
			IDependencyResolver resolver )
		{
			if ( stakhanoviseSetup == null )
				throw new ArgumentNullException( nameof( stakhanoviseSetup ) );

			return stakhanoviseSetup.SetupTaskExecutorRegistry( execRegSetup =>
				execRegSetup.SetupBuiltInTaskExecutorRegistry( builtInExecRegSetup =>
					builtInExecRegSetup.WithResolver( resolver )
				)
			);
		}
	}
}
