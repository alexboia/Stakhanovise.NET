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
using System.Reflection;
using System.Linq;

namespace LVD.Stakhanovise.NET.Executors
{
	public class StandardTaskExecutorRegistry : ITaskExecutorRegistry
	{
		private Dictionary<Type, Type> mMessageExecutorTypes
			  = new Dictionary<Type, Type>();

		private Dictionary<Type, PropertyInfo[]> mMessageExecutorInjectableProperties
			= new Dictionary<Type, PropertyInfo[]>();

		private Dictionary<string, Type> mPayloadTypes
			= new Dictionary<string, Type>();

		private IDependencyResolver mDependencyResolver;

		private static Type mExecutorInterface =
		   typeof( ITaskExecutor<> );

		public StandardTaskExecutorRegistry ( IDependencyResolver dependencyResolver )
		{
			mDependencyResolver = dependencyResolver
				?? throw new ArgumentNullException( nameof( dependencyResolver ) );
		}

		private Type GetImplementedExecutorInterface ( Type type )
		{
			if ( !type.IsClass || type.IsAbstract )
				return null;

			return type.GetInterfaces().FirstOrDefault( i => i.IsGenericType
				 && mExecutorInterface.IsAssignableFrom( i.GetGenericTypeDefinition() ) );
		}

		private bool IsInjectableProperty ( PropertyInfo propertyInfo )
		{
			Type propertyType = propertyInfo.PropertyType;
			//We consider an injectable type as being anything that is not:
			//  - a primitive;
			//  - a value type;
			//  - an array;
			//  - a string type.
			return !propertyType.IsPrimitive
				&& !propertyType.IsValueType
				&& !propertyType.IsArray
				&& !propertyType.Equals( typeof( string ) );
		}

		private void ScanAssembly ( Assembly assembly )
		{
			Type[] executorTypes = assembly.GetTypes();

			foreach ( Type candidateType in executorTypes )
			{
				//See if the candidate type implements ITaskExecutor<> and that is a non-abstract class;
				//  if not, skip it
				Type implementedInterface = GetImplementedExecutorInterface( candidateType );
				if ( implementedInterface == null )
					continue;

				//Fetch the generic argument - this is the payload type
				Type payloadType = implementedInterface.GenericTypeArguments.FirstOrDefault();
				if ( payloadType == null )
					continue;

				PropertyInfo[] injectableProperties = candidateType
					.GetProperties( BindingFlags.Public | BindingFlags.Instance )
					.Where( p => p.CanRead && p.CanWrite && IsInjectableProperty( p ) )
					.ToArray();

				//Register these two pieces of information for later use
				mPayloadTypes[ payloadType.FullName ] = payloadType;
				mMessageExecutorTypes[ payloadType ] = candidateType;

				if ( injectableProperties != null && injectableProperties.Length > 0 )
					mMessageExecutorInjectableProperties[ candidateType ] = injectableProperties;
			}
		}

		public void ScanAssemblies ( params Assembly[] assemblies )
		{
			if ( assemblies != null && assemblies.Length > 0 )
			{
				foreach ( Assembly assembly in assemblies )
				{
					if ( assembly != null )
						ScanAssembly( assembly );
				}
			}
		}

		public ITaskExecutor<TPayload> ResolveExecutor<TPayload> ()
		{
			return ResolveExecutor( payloadType: typeof( TPayload ) )
				as ITaskExecutor<TPayload>;
		}

		public ITaskExecutor ResolveExecutor ( Type payloadType )
		{
			Type executorType;

			PropertyInfo[] injectableProperties;
			ITaskExecutor executorInstance;

			if ( mMessageExecutorTypes.TryGetValue( payloadType, out executorType ) )
			{
				//Create executor instance, if a type is found for the payload type
				executorInstance = ( ITaskExecutor )Activator
					.CreateInstance( executorType );

				//If we have any injectable properties, 
				//  attempt to resolve values and inject them accordingly
				if ( mMessageExecutorInjectableProperties.TryGetValue( executorType, out injectableProperties ) )
				{
					foreach ( PropertyInfo prop in injectableProperties )
						prop.SetValue( executorInstance, mDependencyResolver.TryResolve( prop.PropertyType ) );
				}
			}
			else
				executorInstance = null;

			//Return resolved instance
			return executorInstance;
		}

		public Type ResolvePayloadType ( string typeName )
		{
			if ( string.IsNullOrEmpty( typeName ) )
				return null;

			Type type;
			if ( !mPayloadTypes.TryGetValue( typeName, out type ) )
				type = null;

			return type;
		}

		public IEnumerable<Type> DetectedPayloadTypes
			=> mPayloadTypes.Values;
	}
}
