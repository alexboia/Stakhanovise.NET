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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LVD.Stakhanovise.NET.Options;
using Npgsql;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class ConnectionStringExtensions
	{
		public static string DeriveSignalingConnectionString ( this string connectionString, TaskQueueConsumerOptions options )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder( connectionString );
			return builder.DeriveSignalingConnectionString( options );
		}

		public static string DeriveSignalingConnectionString ( this NpgsqlConnectionStringBuilder info, TaskQueueConsumerOptions options )
		{
			if ( info == null )
				throw new ArgumentNullException( nameof( info ) );

			//The connection used for signaling will be 
			//  the same as the one used for read-only queue operation 
			//  with the notable exceptions that: 
			//  a) we need  to activate the Npgsql keepalive mechanism (see: http://www.npgsql.org/doc/keepalive.html)
			//  b) we do not need a large pool - one connection will do

			NpgsqlConnectionStringBuilder signalingConnectionStringInfo = info.Copy();

			signalingConnectionStringInfo.Pooling = true;
			signalingConnectionStringInfo.MinPoolSize = 1;
			signalingConnectionStringInfo.MaxPoolSize = 2;
			signalingConnectionStringInfo.KeepAlive = options.ConnectionOptions.ConnectionKeepAliveSeconds;

			return signalingConnectionStringInfo.ToString();
		}
	}
}
