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
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class NoOpLogger : IStakhanoviseLogger
	{
		public static readonly NoOpLogger Instance = new NoOpLogger();

		private NoOpLogger()
		{
			return;
		}
		public void Trace ( string message )
		{
			return;
		}

		public void TraceFormat ( string messageFormat, params object[] args )
		{
			return;
		}


		public void Debug ( string message )
		{
			return;
		}

		public void DebugFormat ( string messageFormat, params object[] args )
		{
			return;
		}

		public void Error ( string message )
		{
			return;
		}

		public void Error ( string message, Exception exception )
		{
			return;
		}

		public void Fatal ( string message )
		{
			return;
		}

		public void Fatal ( string message, Exception exception )
		{
			return;
		}

		public void Info ( string message )
		{
			return;
		}

		public void InfoFormat ( string messageFormat, params object[] args )
		{
			return;
		}

		public void Warn ( string message )
		{
			return;
		}

		public void Warn ( string message, Exception exception )
		{
			return;
		}

		public void WarnFormat ( string message, params object[] args )
		{
			return;
		}

		public bool IsEnabled ( StakhanoviseLogLevel level )
		{
			return true;
		}
	}
}
