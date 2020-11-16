// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
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
using LVD.Stakhanovise.NET.Executors;
using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.IoC.NInject
{
	public class NInjectDependencyResolver : IDependencyResolver
	{
		private bool mIsDisposed;

		private IKernel mKernel;

		public NInjectDependencyResolver ()
		{
			mKernel = new StandardKernel();
		}

		public NInjectDependencyResolver ( IEnumerable<INinjectModule> existingModules )
		{
			if ( existingModules == null )
				throw new ArgumentNullException( nameof( existingModules ) );

			mKernel.Load( existingModules );
		}

		public NInjectDependencyResolver ( IKernel existingKernel )
		{
			if ( existingKernel == null )
				throw new ArgumentNullException( nameof( existingKernel ) );

			mKernel = existingKernel;
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( NInjectDependencyResolver ), "Cannot use a disposed Ninject dependency resolver" );
		}

		public bool CanResolve<T> () where T : class
		{
			CheckNotDisposedOrThrow();
			return mKernel.CanResolve<T>();
		}

		public bool CanResolve ( Type serviceType )
		{
			CheckNotDisposedOrThrow();
			return mKernel.CanResolve( serviceType );
		}

		public void Load ( IEnumerable<DependencyRegistration> registrations )
		{
			if ( registrations == null )
				throw new ArgumentNullException( nameof( registrations ) );

			CheckNotDisposedOrThrow();
			mKernel.Load( new StakhanoviseNInjectModule( this, registrations ) );
		}

		public T TryResolve<T> () where T : class
		{
			CheckNotDisposedOrThrow();
			return mKernel.TryGet<T>();
		}

		public object TryResolve ( Type serviceType )
		{
			CheckNotDisposedOrThrow();
			return mKernel.TryGet( serviceType );
		}

		public void Dispose ()
		{
			Dispose( true );
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					if ( mKernel != null )
						mKernel.Dispose();

					mKernel = null;
				}

				mIsDisposed = true;
			}
		}
	}
}
