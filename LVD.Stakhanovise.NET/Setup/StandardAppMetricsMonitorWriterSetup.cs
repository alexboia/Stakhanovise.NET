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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardAppMetricsMonitorWriterSetup : IAppMetricsMonitorWriterSetup
	{
		private Func<IAppMetricsMonitorWriter> mWriterFactory;

		private StandardPostgreSqlAppMetricsMonitorWriterSetup mBuiltInWriterSetup;

		public StandardAppMetricsMonitorWriterSetup( StandardConnectionSetup builtInWriterConnectionSetup,
			StakhanoviseSetupDefaults defaults )
		{
			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mBuiltInWriterSetup = new StandardPostgreSqlAppMetricsMonitorWriterSetup( builtInWriterConnectionSetup,
				defaults );
		}

		public IAppMetricsMonitorWriterSetup SetupBuiltInWriter( Action<IPostgreSqlAppMetricsMonitorWriterSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			if ( mWriterFactory != null )
				throw new InvalidOperationException( "Setting up the built-in writer is not supported when a custom writer has been provided" );

			setupAction.Invoke( mBuiltInWriterSetup );
			return this;
		}

		public IAppMetricsMonitorWriterSetup UseWriter( IAppMetricsMonitorWriter writer )
		{
			if ( writer == null )
				throw new ArgumentNullException( nameof( writer ) );

			return UseWriterFactory( () => writer );
		}

		public IAppMetricsMonitorWriterSetup UseWriterFactory( Func<IAppMetricsMonitorWriter> writerFactory )
		{
			if ( writerFactory == null )
				throw new ArgumentNullException( nameof( writerFactory ) );

			mWriterFactory = writerFactory;
			return this;
		}

		public IAppMetricsMonitorWriterSetup WithMappingForBuiltInWriter( QueuedTaskMapping mapping )
		{
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );
			mBuiltInWriterSetup.WithMapping( mapping );
			return this;
		}

		public IAppMetricsMonitorWriter BuildWriter()
		{
			if ( UseBuiltInWriter )
				return new PostgreSqlAppMetricsMonitorWriter( mBuiltInWriterSetup.BuildOptions() );
			else
				return mWriterFactory.Invoke();
		}

		public bool UseBuiltInWriter => mWriterFactory == null;
	}
}
