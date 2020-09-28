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

namespace LVD.Stakhanovise.NET.Setup
{
	public class CollectiveConnectionSetup : IConnectionSetup
	{
		private StandardConnectionSetup mQueueConsumerConnectionSetup;

		private StandardConnectionSetup mQueueProducerConnectionSetup;

		private StandardConnectionSetup mQueueInfoConnectionSetup;

		private StandardConnectionSetup mBuiltInTimingBeltConnectionSetup;

		private StandardConnectionSetup mBuiltInWriterConnectionSetup;

		public CollectiveConnectionSetup ( StandardConnectionSetup queueConsumerConnectionSetup,
			StandardConnectionSetup queueProducerConnectionSetup,
			StandardConnectionSetup queueInfoConnectionSetup,
			StandardConnectionSetup builtInTimingBeltConnectionSetup,
			StandardConnectionSetup builtInWriterConnectionSetup )
		{
			mQueueConsumerConnectionSetup = queueConsumerConnectionSetup
				?? throw new ArgumentNullException( nameof( queueConsumerConnectionSetup ) );
			mQueueProducerConnectionSetup = queueProducerConnectionSetup
				?? throw new ArgumentNullException( nameof( queueProducerConnectionSetup ) );
			mQueueInfoConnectionSetup = queueInfoConnectionSetup
				?? throw new ArgumentNullException( nameof( queueInfoConnectionSetup ) );
			mBuiltInTimingBeltConnectionSetup = builtInTimingBeltConnectionSetup
				?? throw new ArgumentNullException( nameof( builtInTimingBeltConnectionSetup ) );
			mBuiltInWriterConnectionSetup = builtInWriterConnectionSetup
				?? throw new ArgumentNullException( nameof( builtInWriterConnectionSetup ) );
		}

		public IConnectionSetup WithConnectionKeepAlive ( int connectionKeepAlive )
		{
			mQueueConsumerConnectionSetup.WithConnectionKeepAlive( connectionKeepAlive );
			return this;
		}

		public IConnectionSetup WithConnectionRetryCount ( int connectionRetryCount )
		{
			if ( !mQueueConsumerConnectionSetup.IsConnectionRetryCountUserConfigured )
				mQueueConsumerConnectionSetup.WithConnectionRetryCount( connectionRetryCount );
			
			if ( !mQueueProducerConnectionSetup.IsConnectionRetryCountUserConfigured )
				mQueueProducerConnectionSetup.WithConnectionRetryCount( connectionRetryCount );
			
			if ( !mQueueInfoConnectionSetup.IsConnectionRetryCountUserConfigured )
				mQueueInfoConnectionSetup.WithConnectionRetryCount( connectionRetryCount );
			
			if ( !mBuiltInTimingBeltConnectionSetup.IsConnectionRetryCountUserConfigured )
				mBuiltInTimingBeltConnectionSetup.WithConnectionRetryCount( connectionRetryCount );
			
			if ( !mBuiltInWriterConnectionSetup.IsConnectionRetryCountUserConfigured )
				mBuiltInWriterConnectionSetup.WithConnectionRetryCount( connectionRetryCount );

			return this;
		}

		public IConnectionSetup WithConnectionRetryDelayMilliseconds ( int connectionRetryDelayMilliseconds )
		{
			if ( !mQueueConsumerConnectionSetup.IsConnectionRetryDelayMillisecondsUserConfigured )
				mQueueConsumerConnectionSetup.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );
			
			if ( !mQueueProducerConnectionSetup.IsConnectionRetryDelayMillisecondsUserConfigured )
				mQueueProducerConnectionSetup.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );
			
			if ( !mQueueInfoConnectionSetup.IsConnectionRetryDelayMillisecondsUserConfigured )
				mQueueInfoConnectionSetup.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );
			
			if ( !mBuiltInTimingBeltConnectionSetup.IsConnectionRetryDelayMillisecondsUserConfigured )
				mBuiltInTimingBeltConnectionSetup.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );
			
			if ( !mBuiltInWriterConnectionSetup.IsConnectionRetryDelayMillisecondsUserConfigured )
				mBuiltInWriterConnectionSetup.WithConnectionRetryDelayMilliseconds( connectionRetryDelayMilliseconds );

			return this;
		}

		public IConnectionSetup WithConnectionString ( string connectionString )
		{
			if ( !mQueueConsumerConnectionSetup.IsConnectionStringUserConfigured )
				mQueueConsumerConnectionSetup.WithConnectionString( connectionString );
			
			if ( !mQueueProducerConnectionSetup.IsConnectionStringUserConfigured )
				mQueueProducerConnectionSetup.WithConnectionString( connectionString );
			
			if ( !mQueueInfoConnectionSetup.IsConnectionStringUserConfigured )
				mQueueInfoConnectionSetup.WithConnectionString( connectionString );
			
			if ( !mBuiltInTimingBeltConnectionSetup.IsConnectionStringUserConfigured )
				mBuiltInTimingBeltConnectionSetup.WithConnectionString( connectionString );

			if ( !mBuiltInWriterConnectionSetup.IsConnectionStringUserConfigured )
				mBuiltInWriterConnectionSetup.WithConnectionString( connectionString );

			return this;
		}
	}
}
