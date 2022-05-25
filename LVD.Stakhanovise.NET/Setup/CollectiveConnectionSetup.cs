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

namespace LVD.Stakhanovise.NET.Setup
{
	public class CollectiveConnectionSetup : IConnectionSetup
	{
		private StandardConnectionSetup[] mConnectionSetupTargets;

		public CollectiveConnectionSetup ( params StandardConnectionSetup[] targets )
		{
			if ( targets == null || targets.Length == 0 )
				throw new ArgumentNullException( nameof( targets ) );

			mConnectionSetupTargets = targets;
		}

		public IConnectionSetup WithConnectionKeepAlive ( int connectionKeepAlive )
		{
			foreach ( StandardConnectionSetup cs in mConnectionSetupTargets )
				if ( !cs.IsConnectionKeepAliveSecondsUserConfigured )
					cs.WithConnectionKeepAlive( connectionKeepAlive );

			return this;
		}

		public IConnectionSetup WithConnectionRetryCount ( int connectionRetryCount )
		{
			foreach ( StandardConnectionSetup cs in mConnectionSetupTargets )
				if ( !cs.IsConnectionRetryCountUserConfigured )
					cs.WithConnectionRetryCount( connectionRetryCount );

			return this;
		}

		public IConnectionSetup WithConnectionRetryDelayMilliseconds ( int connectionRetryDelayMilliseconds )
		{
			foreach ( StandardConnectionSetup cs in mConnectionSetupTargets )
				if ( !cs.IsConnectionRetryDelayMillisecondsUserConfigured )
					cs.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );

			return this;
		}

		public IConnectionSetup WithConnectionString ( string connectionString )
		{
			foreach ( StandardConnectionSetup cs in mConnectionSetupTargets )
				if ( !cs.IsConnectionStringUserConfigured )
					cs.WithConnectionString( connectionString );

			return this;
		}
	}
}
