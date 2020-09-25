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
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors.IoC
{
	/// <summary>
	/// Used ASP.NET extensions as a model for this implementation:
	///		https://github.com/grumpydev/TinyIoC/blob/master/src/TinyIoC.AspNetExtensions/TinyIoCAspNetExtensions.cs
	/// </summary>
	public class TinyIoCThreadSingletonLifetimeManager : TinyIoCContainer.ITinyIoCObjectLifetimeProvider
	{
		[ThreadStatic]
		private static object mInstance;

		public object GetObject ()
		{
			return mInstance;
		}

		public void ReleaseObject ()
		{
			IDisposable disposableInstance = mInstance as IDisposable;

			if ( disposableInstance != null )
				disposableInstance.Dispose();

			SetObject( null );
		}

		public void SetObject ( object value )
		{
			mInstance = value;
		}
	}

	public static class TinyIoCThreadSingletonExtensions
	{
		public static TinyIoCContainer.RegisterOptions AsPerRequestSingleton ( this TinyIoCContainer.RegisterOptions registerOptions )
		{
			return TinyIoCContainer.RegisterOptions.ToCustomLifetimeManager( registerOptions, 
				new TinyIoCThreadSingletonLifetimeManager(), 
				"per thread singleton" );
		}

		public static TinyIoCContainer.MultiRegisterOptions AsPerRequestSingleton ( this TinyIoCContainer.MultiRegisterOptions registerOptions )
		{
			return TinyIoCContainer.MultiRegisterOptions.ToCustomLifetimeManager( registerOptions, 
				new TinyIoCThreadSingletonLifetimeManager(), 
				"per thread singleton" );
		}
	}
}
