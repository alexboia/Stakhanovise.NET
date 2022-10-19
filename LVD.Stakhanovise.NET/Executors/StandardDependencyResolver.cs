// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
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
				if ( mContainer.CanResolve( registration.Target ) )
					continue;

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
					mContainer.Register( registration.Target, registration.AsInstance );
				}
			}
		}

		public T TryResolve<T> () where T : class
		{
			if ( mContainer.TryResolve<T>( out T instance ) )
				mContainer.BuildUp( instance );
			else
				instance = default;

			return instance;
		}

		public object TryResolve ( Type serviceType )
		{
			if ( mContainer.TryResolve( serviceType, out object instance ) )
				mContainer.BuildUp( instance );
			else
				instance = null;

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
