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
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.IoC.NInject
{
	public class StakhanoviseNInjectModule : NinjectModule
	{
		private IDependencyResolver mDependencyResolver;

		private IEnumerable<DependencyRegistration> mRegistrations;

		public StakhanoviseNInjectModule ( IDependencyResolver dependencyResolver,
			IEnumerable<DependencyRegistration> registrations )
		{
			mDependencyResolver = dependencyResolver
				?? throw new ArgumentNullException( nameof( dependencyResolver ) );
			mRegistrations = registrations
				?? throw new ArgumentNullException( nameof( registrations ) );
		}

		public override void Load ()
		{
			Bind<IDependencyResolver>()
				.ToConstant( mDependencyResolver );

			foreach ( DependencyRegistration registration in mRegistrations )
			{
				if ( registration.AsImplementationType != null )
				{
					Bind( registration.Target )
						.To( registration.AsImplementationType )
						.InScope( registration.Scope );
				}
				else if ( registration.AsProvider != null )
				{
					//The internal provider will be used to actually create the instance
					//	- the Ninject provider that we're registering below
					//	simply delegates to it the creation of the instance
					Bind( CreateInternalProviderTargetType( registration.Target ) )
						.To( registration.AsProvider )
						.InTransientScope();

					Bind( registration.Target )
						.ToProvider( CreateNinjectProviderWrapperType( registration.Target ) )
						.InScope( registration.Scope );
				}
				else if ( registration.AsProviderInstance != null )
				{
					Bind( CreateInternalProviderTargetType( registration.Target ) )
						.ToConstant( registration.AsProviderInstance )
						.InSingletonScope();

					Bind( registration.Target )
						.ToProvider( CreateNinjectProviderWrapperType( registration.Target ) )
						.InScope( registration.Scope );
				}
				else if ( registration.AsInstance != null )
				{
					Bind( registration.Target )
						.ToConstant( registration.AsInstance );
				}
			}
		}

		private Type CreateNinjectProviderWrapperType ( Type targetType )
		{
			return typeof( GenericNInjectProvider<> )
				.MakeGenericType( targetType );
		}

		private Type CreateInternalProviderTargetType ( Type targetType )
		{
			return typeof( IDependencyProvider<> )
				.MakeGenericType( targetType );
		}
	}
}
