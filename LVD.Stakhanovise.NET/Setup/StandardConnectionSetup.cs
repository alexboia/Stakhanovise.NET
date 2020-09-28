﻿// 
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
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardConnectionSetup : IConnectionSetup
	{
		private string mConnectionString;

		private bool mIsConnectionStringUserConfigured = false;

		private int mConnectionKeepAliveSeconds = 0;

		private bool mIsConnectionKeepAliveSecondsUserConfigured = false;

		private int mConnectionRetryCount = 3;

		private bool mIsConnectionRetryCountUserConfigured = false;

		private int mConnectionRetryDelayMilliseconds = 100;

		private bool mIsConnectionRetryDelayMillisecondsUserConfigured = false;

		public IConnectionSetup WithConnectionKeepAlive ( int connectionKeepAliveSeconds )
		{
			mConnectionKeepAliveSeconds = connectionKeepAliveSeconds;
			mIsConnectionKeepAliveSecondsUserConfigured = true;
			return this;
		}

		public IConnectionSetup WithConnectionRetryCount ( int connectionRetryCount )
		{
			mConnectionRetryCount = connectionRetryCount;
			mIsConnectionRetryCountUserConfigured = true;
			return this;
		}

		public IConnectionSetup WithConnectionRetryDelayMilliseconds ( int connectionRetryDelayMilliseconds )
		{
			mConnectionRetryDelayMilliseconds = connectionRetryDelayMilliseconds;
			mIsConnectionRetryDelayMillisecondsUserConfigured = true;
			return this;
		}

		public IConnectionSetup WithConnectionString ( string connectionString )
		{
			if ( string.IsNullOrEmpty( connectionString ) )
				throw new ArgumentNullException( nameof( connectionString ) );

			mConnectionString = connectionString;
			mIsConnectionStringUserConfigured = true;
			return this;
		}

		public ConnectionOptions BuildOptions()
		{
			return new ConnectionOptions( mConnectionString, 
				mConnectionKeepAliveSeconds, 
				mConnectionRetryCount, 
				mConnectionRetryDelayMilliseconds );
		}

		public bool IsConnectionStringUserConfigured 
			=> mIsConnectionStringUserConfigured;

		public bool IsConnectionKeepAliveSecondsUserConfigured
			=> mIsConnectionKeepAliveSecondsUserConfigured;

		public bool IsConnectionRetryCountUserConfigured 
			=> mIsConnectionRetryCountUserConfigured;

		public bool IsConnectionRetryDelayMillisecondsUserConfigured
			=> mIsConnectionRetryDelayMillisecondsUserConfigured;
	}
}
