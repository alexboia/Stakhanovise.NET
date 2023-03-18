// 
// BSD 3-Clause License
// 
// Copyright (c) 2020 - 2023, Boia Alexandru
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
using Newtonsoft.Json;
using System;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class SerializationExtensions
	{
		public static string ToJson( this object sourceObject,
			bool includeTypeInformation = false )
		{
			Action<JsonSerializerSettings> noOpConfig = DelegateHelpers
				.CreateNoOpAction<JsonSerializerSettings>();

			return sourceObject.ToJson( noOpConfig,
				includeTypeInformation );
		}

		public static string ToJson( this object sourceObject,
			Action<JsonSerializerSettings> configureSerializer,
			bool includeTypeInformation = false )
		{
			if ( sourceObject == null )
				return null;

			if ( configureSerializer == null )
				throw new ArgumentNullException( nameof( configureSerializer ) );

			JsonSerializerSettings settings =
				new JsonSerializerSettings();

			configureSerializer.Invoke( settings );
			if ( includeTypeInformation )
				settings.TypeNameHandling = TypeNameHandling.All;

			return JsonConvert.SerializeObject( sourceObject, settings );
		}

		public static T AsObjectFromJson<T>( this string sourceString,
			Action<JsonSerializerSettings> configureSerializer )
		{
			if ( string.IsNullOrEmpty( sourceString ) )
				return default( T );

			if ( configureSerializer == null )
				throw new ArgumentNullException( nameof( configureSerializer ) );

			JsonSerializerSettings settings =
				CreateDeserializerSettings( configureSerializer );

			return JsonConvert.DeserializeObject<T>( sourceString,
				settings );
		}

		private static JsonSerializerSettings CreateDeserializerSettings( Action<JsonSerializerSettings> configureSerializer )
		{
			JsonSerializerSettings settings =
				new JsonSerializerSettings();

			configureSerializer
				.Invoke( settings );

			settings.ConstructorHandling = ConstructorHandling
				.AllowNonPublicDefaultConstructor;
			settings.TypeNameHandling = TypeNameHandling
				.Auto;

			return settings;
		}

		public static T AsObjectFromJson<T>( this string sourceString )
		{
			Action<JsonSerializerSettings> noOpConfig = DelegateHelpers
				.CreateNoOpAction<JsonSerializerSettings>();
			return sourceString
				.AsObjectFromJson<T>( noOpConfig );
		}

		public static object AsObjectFromJson( this string sourceString,
			Action<JsonSerializerSettings> configureSerializer )
		{
			if ( string.IsNullOrEmpty( sourceString ) )
				return null;

			if ( configureSerializer == null )
				throw new ArgumentNullException( nameof( configureSerializer ) );

			JsonSerializerSettings settings =
				CreateDeserializerSettings( configureSerializer );

			return JsonConvert.DeserializeObject( sourceString,
				settings );
		}

		public static object AsObjectFromJson( this string sourceString )
		{
			Action<JsonSerializerSettings> noOpConfig = DelegateHelpers
				.CreateNoOpAction<JsonSerializerSettings>();
			return sourceString
				.AsObjectFromJson( noOpConfig );
		}
	}
}
