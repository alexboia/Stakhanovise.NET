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
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StakhanoviseSetup : IStakhanoviseSetup
	{
		private bool mAppMetricsMonitoringEnabled;

		private Assembly[] mExecutorAssemblies = null;

		private StandardTaskExecutorRegistrySetup mTaskExecutorRegistrySetup =
			new StandardTaskExecutorRegistrySetup();

		private StandardTaskQueueConsumerSetup mTaskQueueConsumerSetup;

		private StandardTaskQueueProducerSetup mTaskQueueProducerSetup;

		private StandardTaskQueueInfoSetup mTaskQueueInfoSetup;

		private CollectiveConnectionSetup mCommonTaskQueueConnectionSetup;

		private StandardTaskEngineSetup mTaskEngineSetup;

		private StandardExecutionPerformanceMonitorWriterSetup mPerformanceMonitorWriterSetup;

		private StandardAppMetricsMonitorWriterSetup mAppMetricsMonitorWriterSetup;

		private StandardAppMetricsMonitorSetup mAppMetricsMonitorSetup;

		private IStakhanoviseLoggingProvider mLoggingProvider;

		private bool mRegisterOwnDependencies = true;

		public StakhanoviseSetup ( StakhanoviseSetupDefaults defaults )
		{
			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mAppMetricsMonitoringEnabled = defaults
				.AppMetricsMonitoringEnabled;

			StandardConnectionSetup queueConsumerConnectionSetup =
				new StandardConnectionSetup();

			StandardConnectionSetup queueProducerConnectionSetup =
				new StandardConnectionSetup();

			StandardConnectionSetup queueInfoConnectionSetup =
				new StandardConnectionSetup();

			StandardConnectionSetup builtInTimingBeltConnectionSetup =
				new StandardConnectionSetup();

			StandardConnectionSetup builtInWriterConnectionSetup =
				new StandardConnectionSetup();

			StandardConnectionSetup builtInAppMetricsWriterConnectionSetup =
				new StandardConnectionSetup();

			mTaskQueueProducerSetup = new StandardTaskQueueProducerSetup( queueProducerConnectionSetup,
				defaultMapping: defaults.Mapping );

			mTaskQueueInfoSetup = new StandardTaskQueueInfoSetup( queueInfoConnectionSetup,
				defaultMapping: defaults.Mapping );

			mCommonTaskQueueConnectionSetup = new CollectiveConnectionSetup( queueConsumerConnectionSetup,
				queueProducerConnectionSetup,
				queueInfoConnectionSetup,
				builtInTimingBeltConnectionSetup,
				builtInWriterConnectionSetup,
				builtInAppMetricsWriterConnectionSetup );

			mTaskQueueConsumerSetup = new StandardTaskQueueConsumerSetup( queueConsumerConnectionSetup,
				defaults );

			mTaskEngineSetup = new StandardTaskEngineSetup( mTaskQueueConsumerSetup,
				defaults );

			mPerformanceMonitorWriterSetup = new StandardExecutionPerformanceMonitorWriterSetup( builtInWriterConnectionSetup,
				defaults );

			mAppMetricsMonitorSetup = new StandardAppMetricsMonitorSetup( defaults );

			mAppMetricsMonitorWriterSetup = new StandardAppMetricsMonitorWriterSetup( builtInAppMetricsWriterConnectionSetup,
				defaults );
		}

		public IStakhanoviseSetup SetupEngine ( Action<ITaskEngineSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskEngineSetup );
			return this;
		}

		public IStakhanoviseSetup SetupPerformanceMonitorWriter ( Action<IExecutionPerformanceMonitorWriterSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mPerformanceMonitorWriterSetup );
			return this;
		}

		public IStakhanoviseSetup DisableAppMetricsMonitoring ()
		{
			mAppMetricsMonitoringEnabled = false;
			return this;
		}

		public IStakhanoviseSetup SetupAppMetricsMonitorWriter ( Action<IAppMetricsMonitorWriterSetup> setupAction )
		{

			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mAppMetricsMonitorWriterSetup );
			return this;
		}

		public IStakhanoviseSetup SetupAppMetricsMonitor ( Action<IAppMetricsMonitorSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mAppMetricsMonitorSetup );
			return this;
		}

		public IStakhanoviseSetup SetupTaskExecutorRegistry ( Action<ITaskExecutorRegistrySetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskExecutorRegistrySetup );
			return this;
		}

		public IStakhanoviseSetup WithExecutorAssemblies ( params Assembly[] assemblies )
		{
			if ( assemblies == null || assemblies.Length == 0 )
				throw new ArgumentNullException( nameof( assemblies ) );

			mExecutorAssemblies = assemblies;
			return this;
		}

		public IStakhanoviseSetup SetupTaskQueueConnection ( Action<IConnectionSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mCommonTaskQueueConnectionSetup );
			return this;
		}

		public IStakhanoviseSetup WithTaskQueueMapping ( QueuedTaskMapping mapping )
		{
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			mTaskQueueConsumerSetup.WithMapping( mapping );
			mTaskQueueProducerSetup.WithMapping( mapping );
			mTaskQueueInfoSetup.WithMapping( mapping );

			mPerformanceMonitorWriterSetup.WithMappingForBuiltInWriter( mapping );
			mAppMetricsMonitorWriterSetup.WithMappingForBuiltInWriter( mapping );

			return this;
		}

		public IStakhanoviseSetup SetupTaskQueueConsumer ( Action<ITaskQueueConsumerSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskQueueConsumerSetup );
			return this;
		}

		public IStakhanoviseSetup SetupTaskQueueProducer ( Action<ITaskQueueProducerSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskQueueProducerSetup );
			return this;
		}

		public IStakhanoviseSetup SetupTaskQueueInfo ( Action<ITaskQueueInfoSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mTaskQueueInfoSetup );
			return this;
		}

		public IStakhanoviseSetup WithLoggingProvider ( IStakhanoviseLoggingProvider loggingProvider )
		{
			if ( loggingProvider == null )
				throw new ArgumentNullException( nameof( loggingProvider ) );

			mLoggingProvider = loggingProvider;
			return this;
		}

		public IStakhanoviseSetup WithConsoleLogging ( StakhanoviseLogLevel level, bool writeToStdOut = false )
		{
			return WithLoggingProvider( new ConsoleLoggingProvider( level,
				writeToStdOut ) );
		}

		public IStakhanoviseSetup DontRegisterOwnDependencies ()
		{
			mRegisterOwnDependencies = false;
			return this;
		}

		public ITaskEngine BuildTaskEngine ()
		{
			ITaskExecutorRegistry executorRegistry = mTaskExecutorRegistrySetup
				.BuildTaskExecutorRegistry();

			IExecutionPerformanceMonitorWriter executionPerfMonWriter = mPerformanceMonitorWriterSetup
				.BuildWriter();

			ITimestampProvider timestampProvider =
				new UtcNowTimestampProvider();

			TaskQueueConsumerOptions consumerOptions = mTaskQueueConsumerSetup
				.BuildOptions();

			TaskQueueOptions producerOptions = mTaskQueueProducerSetup
				.BuildOptions();

			StakhanoviseLogManager.Provider = mLoggingProvider
				?? new NoOpLoggingProvider();

			ITaskQueueProducer taskQueueProducer = new PostgreSqlTaskQueueProducer( producerOptions,
				timestampProvider );

			ITaskQueueInfo taskQueueInfo = new PostgreSqlTaskQueueInfo( mTaskQueueInfoSetup.BuiltOptions(),
				timestampProvider );

			if ( mRegisterOwnDependencies )
			{
				executorRegistry.LoadDependencies( new Dictionary<Type, object>()
				{
					{ typeof(ITaskQueueProducer),
						taskQueueProducer },
					{ typeof(ITaskQueueInfo),
						taskQueueInfo }
				} );
			}

			return mTaskEngineSetup.BuildTaskEngine( consumerOptions,
				producerOptions,
				executorRegistry,
				executionPerfMonWriter,
				timestampProvider );
		}

		public IAppMetricsMonitor BuildAppMetricsMonitor ()
		{
			return mAppMetricsMonitoringEnabled
				? mAppMetricsMonitorSetup.BuildMonitor( mAppMetricsMonitorWriterSetup.BuildWriter() )
				: null;
		}
	}
}
