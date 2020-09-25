using LVD.Stakhanovise.NET.Executors.IoC;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors
{
	public class StandardDependencyResolver : IDependencyResolver
	{
		private bool mIsDisposed = false;

		private TinyIoCContainer mContainer = new TinyIoCContainer();

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardDependencyResolver ),
					"Cannot reuse a disposed standard dependency resolver" );
		}

		public bool CanResolve<T> () where T : class
		{
			return mContainer.CanResolve<T>();
		}

		public bool CanResolve ( Type serviceType )
		{
			return mContainer.CanResolve( serviceType );
		}

		public void Load ( IEnumerable<DependencyRegistration> registrations )
		{
			foreach ( DependencyRegistration registration in registrations )
			{
				if ( registration.AsImplementationType != null )
				{
					mContainer.Register( registration.Target, registration.AsImplementationType )
						.WithScopeFromRegistrationInfo( registration );
				}
				else if ( registration.AsProvider != null )
				{
					mContainer.RegisterWithProviderType( registration.Target,
						registration.AsProvider,
						this ).WithScopeFromRegistrationInfo( registration );
				}
				else if ( registration.AsProviderInstance != null )
				{
					mContainer.RegisterWithProviderInstance( registration.Target,
						registration.AsProviderInstance as IDependencyProvider,
						this ).WithScopeFromRegistrationInfo( registration );
				}
				else if ( registration.AsInstance != null )
				{
					mContainer.Register( registration.Target, registration.AsInstance )
						.AsSingleton();
				}
			}
		}

		public T TryResolve<T> () where T : class
		{
			if ( !mContainer.TryResolve<T>( out T instance ) )
				instance = default;
			else
				mContainer.BuildUp( instance );

			return instance;
		}

		public object TryResolve ( Type serviceType )
		{
			if ( !mContainer.TryResolve( serviceType, out object instance ) )
				instance = null;
			else
				mContainer.BuildUp( instance );

			return instance;
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					mContainer.Dispose();
					mContainer = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
	}
}
