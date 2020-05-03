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
using Newtonsoft.Json;

namespace LVD.Stakhanovise.NET.Helpers
{
   public static class SerializationExtensions
   {
      public static List<T> AsListFromJson<T>(this string sourceString)
      {
         return sourceString.AsObjectFromJson<List<T>>();
      }

      public static string ToJson(this object sourceObject, bool includeTypeInformation = false)
      {
         if (sourceObject == null)
            return null;

         JsonSerializerSettings settings = new JsonSerializerSettings();

         if (includeTypeInformation)
            settings.TypeNameHandling = TypeNameHandling.All;

         return JsonConvert.SerializeObject(sourceObject, settings);
      }

      public static T AsObjectFromJson<T>(this string sourceString)
      {
         if (string.IsNullOrEmpty(sourceString))
            return default(T);

         JsonSerializerSettings settings = new JsonSerializerSettings();

         settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
         settings.TypeNameHandling = TypeNameHandling.Auto;

         return JsonConvert.DeserializeObject<T>(sourceString, settings);
      }

      public static object AsObjectFromJson(this string sourceString)
      {
         if (string.IsNullOrEmpty(sourceString))
            return null;

         JsonSerializerSettings settings = new JsonSerializerSettings();

         settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
         settings.TypeNameHandling = TypeNameHandling.Auto;

         return JsonConvert.DeserializeObject(sourceString, settings);
      }
   }
}
