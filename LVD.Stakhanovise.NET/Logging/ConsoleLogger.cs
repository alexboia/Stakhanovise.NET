// 
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
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging
{
	public class ConsoleLogger : IStakhanoviseLogger
	{
		private string mName;

		private StakhanoviseLogLevel mMinLevel;

		private bool mWriteToStdOut = false;

		public ConsoleLogger ( StakhanoviseLogLevel minLevel, string name, bool writeToStdOut = false )
		{
			mName = name;
			mMinLevel = minLevel;
			mWriteToStdOut = writeToStdOut;
		}

		private void Log ( StakhanoviseLogLevel level, string message, Exception exception = null )
		{
			if ( !IsEnabled( level ) )
				return;

			StringBuilder logMessage =
				new StringBuilder();

			string dateTime = DateTimeOffset.Now
				.ToString( "yyyy-MM-dd HH:mm:ss zzz" );

			logMessage.Append( $"[{dateTime}]" )
				.Append( " - " );

			if ( !string.IsNullOrEmpty( mName ) )
				logMessage.Append( $"{mName}" )
					.Append( " - " );

			logMessage.Append( $"{level.ToString()}" );

			if ( !string.IsNullOrEmpty( message ) )
				logMessage.Append( " - " ).Append( message );

			if ( exception != null )
			{
				logMessage
					.Append( " - " )
					.Append( exception.GetType().FullName )
					.Append( ": " )
					.Append( exception.Message )
					.Append( ":" )
					.Append( exception.StackTrace );
			}

			if ( !mWriteToStdOut )
				Console.Error.WriteLine( logMessage.ToString() );
			else
				Console.WriteLine( logMessage );
		}

		public void Trace ( string message )
		{
			Log( StakhanoviseLogLevel.Trace, message );
		}

		public void TraceFormat ( string messageFormat, params object[] args )
		{
			Trace( string.Format( messageFormat, args ) );
		}

		public void Debug ( string message )
		{
			Log( StakhanoviseLogLevel.Debug, message );
		}

		public void DebugFormat ( string messageFormat, params object[] args )
		{
			Debug( string.Format( messageFormat, args ) );
		}

		public void Error ( string message )
		{
			Log( StakhanoviseLogLevel.Error, message );
		}

		public void Error ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Error, message, exception );
		}

		public void Fatal ( string message )
		{
			Log( StakhanoviseLogLevel.Fatal, message );
		}

		public void Fatal ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Fatal, message, exception );
		}

		public void Info ( string message )
		{
			Log( StakhanoviseLogLevel.Info, message );
		}

		public void InfoFormat ( string messageFormat, params object[] args )
		{
			Info( string.Format( messageFormat, args ) );
		}

		public void Warn ( string message )
		{
			Log( StakhanoviseLogLevel.Warn, message );
		}

		public void Warn ( string message, Exception exception )
		{
			Log( StakhanoviseLogLevel.Warn, message, exception );
		}

		public void WarnFormat ( string messageFormat, params object[] args )
		{
			Warn( string.Format( messageFormat, args ) );
		}

		public bool IsEnabled ( StakhanoviseLogLevel level )
		{
			return level >= mMinLevel;
		}
	}
}
