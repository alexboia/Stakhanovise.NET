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
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardTaskEngineSetup : ITaskEngineSetup
	{
		private int mWorkerCount;

		private StadardTaskProcessingSetup mTaskProcessingSetup;

		private StandardExecutionPerformanceMonitorSetup mExecutionPerformanceMonitorSetup;

		private StandardTaskQueueConsumerSetup mTaskQueueConsumerSetup;

		public StandardTaskEngineSetup ( StandardTaskQueueConsumerSetup taskQueueConsumerSetup, StakhanoviseSetupDefaults defaults )
		{
			if ( taskQueueConsumerSetup == null )
				throw new ArgumentNullException( nameof( taskQueueConsumerSetup ) );

			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mWorkerCount = defaults.WorkerCount;
			mTaskProcessingSetup = new StadardTaskProcessingSetup( defaults );
			mTaskQueueConsumerSetup = taskQueueConsumerSetup;
			mExecutionPerformanceMonitorSetup = new StandardExecutionPerformanceMonitorSetup( defaults );
		}

		public ITaskEngineSetup SetupPerformanceMonitor ( Action<IExecutionPerformanceMonitorSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mExecutionPerformanceMonitorSetup );
			return this;
		}

		public ITaskEngineSetup SetupTaskProcessing ( Action<ITaskProcessingSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskProcessingSetup );
			return this;
		}

		public ITaskEngineSetup WithWorkerCount ( int workerCount )
		{
			if ( workerCount < 1 )
				throw new ArgumentOutOfRangeException( nameof( workerCount ),
					"Worker count must be greater than 1" );

			mWorkerCount = workerCount;
			if ( !mTaskQueueConsumerSetup.IsQueueConsumerConnectionPoolSizeUserConfigured )
				mTaskQueueConsumerSetup.WithQueueConsumerConnectionPoolSize( mWorkerCount * 2 );

			return this;
		}

		private TaskEngineOptions BuildOptions ()
		{
			return new TaskEngineOptions( mWorkerCount,
				mExecutionPerformanceMonitorSetup.BuildOptions(),
				mTaskProcessingSetup.BuildOptions() );
		}

		public ITaskEngine BuildTaskEngine ( TaskQueueConsumerOptions consumerOptions,
			ITaskExecutorRegistry executorRegistry,
			ITaskQueueTimingBelt timingBelt,
			IExecutionPerformanceMonitorWriter executionPerfMonWriter )
		{
			if ( consumerOptions == null )
				throw new ArgumentNullException( nameof( consumerOptions ) );

			if ( executorRegistry == null )
				throw new ArgumentNullException( nameof( executorRegistry ) );

			if ( executionPerfMonWriter == null )
				throw new ArgumentNullException( nameof( executionPerfMonWriter ) );

			if ( timingBelt == null )
				throw new ArgumentNullException( nameof( timingBelt ) );

			return new StandardTaskEngine( BuildOptions(), consumerOptions,
				executorRegistry,
				timingBelt,
				executionPerfMonWriter );
		}
	}
}
