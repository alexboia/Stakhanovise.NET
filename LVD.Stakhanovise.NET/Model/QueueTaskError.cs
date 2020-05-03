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

namespace LVD.Stakhanovise.NET.Model
{
   public class QueueTaskError
   {
      private QueueTaskError()
      {
         return;
      }

      public QueueTaskError(string message)
          : this(null, message, null)
      {
         return;
      }

      public QueueTaskError(string type, string message)
         : this(type, message, null)
      {
         return;
      }

      public QueueTaskError(string type, string message, string stackTrace)
          : this()
      {
         Type = type;
         Message = message;
         StackTrace = stackTrace;
      }

      public QueueTaskError(Exception exc)
          : this(exc.GetType().FullName, exc.Message, exc.StackTrace)
      {
         return;
      }

      public static QueueTaskError FromException(Exception exc)
      {
         return new QueueTaskError(exc);
      }

      public static QueueTaskError FromMessage(string message)
      {
         return new QueueTaskError(message);
      }

      public static QueueTaskError FromMessage(string type, string message)
      {
         return new QueueTaskError(type, message);
      }

      public bool Equals(QueueTaskError taskError)
      {
         return taskError != null
             && string.Equals(Type, taskError.Type)
             && string.Equals(Message, taskError.Message)
             && string.Equals(StackTrace, taskError.StackTrace);
      }

      public override bool Equals(object obj)
      {
         return Equals(obj as QueueTaskError);
      }

      public override int GetHashCode()
      {
         int result = 1;

         result = result * 13 + Type.GetHashCode();
         result = result * 13 + Message.GetHashCode();
         result = result * 13 + StackTrace.GetHashCode();

         return result;
      }

      public string Type { get; set; }

      public string Message { get; set; }

      public string StackTrace { get; set; }
   }
}
