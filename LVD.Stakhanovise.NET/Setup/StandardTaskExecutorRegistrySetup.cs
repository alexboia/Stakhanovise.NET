﻿// 
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
using LVD.Stakhanovise.NET.Executors;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardTaskExecutorRegistrySetup : ITaskExecutorRegistrySetup
	{
		private Func<ITaskExecutorRegistry> mTaskExecutorRegistryFactory;

		private StandardStandardTaskExecutorRegistrySetup mBuiltInTaskExecutorRegistrySetup =
			new StandardStandardTaskExecutorRegistrySetup();

		public ITaskExecutorRegistrySetup SetupBuiltInTaskExecutorRegistry( Action<IStandardTaskExecutorRegistrySetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			if ( mBuiltInTaskExecutorRegistrySetup == null )
				throw new InvalidOperationException( "Setting up the built-in executor registry is not supported when a custom executor registry has been provided" );

			setupAction.Invoke( mBuiltInTaskExecutorRegistrySetup );
			return this;
		}

		public ITaskExecutorRegistrySetup UseTaskExecutorRegistry( ITaskExecutorRegistry registry )
		{
			if ( registry == null )
				throw new ArgumentNullException( nameof( registry ) );

			return UseTaskExecutorRegistryFactory( () => registry );
		}

		public ITaskExecutorRegistrySetup UseTaskExecutorRegistryFactory( Func<ITaskExecutorRegistry> registryFactory )
		{
			if ( registryFactory == null )
				throw new ArgumentNullException( nameof( registryFactory ) );

			mTaskExecutorRegistryFactory = registryFactory;
			return this;
		}

		public ITaskExecutorRegistry BuildTaskExecutorRegistry()
		{
			if ( UseBuiltInTaskExecutorRegistry )
				return new StandardTaskExecutorRegistry( mBuiltInTaskExecutorRegistrySetup
					.BuildDependencyResolver() );
			else
				//TODO: should be able to pass dependency resolver
				//	to task executor registry.
				return mTaskExecutorRegistryFactory.Invoke();
		}

		public bool UseBuiltInTaskExecutorRegistry
			=> mTaskExecutorRegistryFactory == null;
	}
}
