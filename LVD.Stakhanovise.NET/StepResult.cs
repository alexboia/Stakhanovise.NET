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

namespace LVD.Stakhanovise.NET
{
   public class StepResult
   {
      protected StepResult(Exception exception, string message)
      {
         Success = false;
         Exception = exception;
         Message = message;
      }

      protected StepResult(object data)
          : this()
      {
         Data = data;
      }

      protected StepResult()
      {
         Success = true;
      }

      public bool DataIs<T>()
      {
         return Data != null && Data is T;
      }

      public T DataAs<T>()
      {
         if (Data is T)
            return (T)Data;
         else
            return default(T);
      }

      public static StepResult Successful()
          => new StepResult();

      public static StepResult Successful(object data)
          => new StepResult(data);

      public static StepResult FromErrorFormat(string message, params object[] args)
          => new StepResult(null, string.Format(message, args));

      public static StepResult FromError(string message = null)
          => new StepResult(null, message);

      public static StepResult FromException(Exception exc)
          => new StepResult(exc, null);

      public bool Success { get; protected set; }

      public Exception Exception { get; protected set; }

      public string Message { get; protected set; }

      public object Data { get; protected set; }
   }
}
