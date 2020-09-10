using LVD.Stakhanovise.NET.Tests.Executors;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class NinjectTasksTestModule : NinjectModule
	{
		public override void Load ()
		{
			Bind<ISampleExecutorDependency>().To<SampleExecutorDependencyImpl>();
		}
	}
}
