using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors
{
	public interface IDependencyResolver : IDisposable
	{
		T TryResolve<T> () where T: class;

		object TryResolve ( Type serviceType );

		bool CanResolve<T> () where T : class;

		bool CanResolve ( Type serviceType );

		void Load ( IEnumerable<DependencyRegistration> registration );
	}
}
