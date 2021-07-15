// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
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
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IDependencySetup
	{
		IDependencySetup BindToType<T, TImplementation> ( DependencyScope scope )
			where TImplementation : T;

		IDependencyBindingScopeSetup BindToType<T, TImplementation> ()
			where TImplementation : T;

		IDependencySetup BindToInstance<T> ( T instance );

		IDependencySetup BindToSelf<T> ( DependencyScope scope );

		IDependencyBindingScopeSetup BindToSelf<T> ();

		IDependencySetup BindToProvider<T, TImplementation, TProvider> ( TImplementation instance,
			DependencyScope scope )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>;

		IDependencyBindingScopeSetup BindToProvider<T, TImplementation, TProvider> ( TImplementation instance )
			where TImplementation : T
			where TProvider : IDependencyProvider<TImplementation>;

		IDependencySetup BindToProviderInstance<T, TImplementation> ( IDependencyProvider<TImplementation> implementationProvider,
			DependencyScope scope )
			where TImplementation : T;

		IDependencyBindingScopeSetup BindToProviderInstance<T, TImplementation> ( IDependencyProvider<TImplementation> implementationProvider )
			where TImplementation : T;

		IDependencySetup BindToType ( Type target,
			Type implementationType,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToType ( Type target,
			Type implementationType );

		IDependencySetup BindToInstance ( Type target,
			object instance );

		IDependencySetup BindToSelf ( Type target,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToSelf ( Type target );

		IDependencySetup BindToProvider ( Type target,
			Type providerType,
			DependencyScope scope );

		IDependencyBindingScopeSetup BindToProvider ( Type target,
			Type providerType );

		bool HasBindingFor<T> ();

		bool HasBindingFor ( Type target );
	}
}
