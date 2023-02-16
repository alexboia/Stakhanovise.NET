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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Setup;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings
{
	public class NetCoreConfigurationStakhanoviseDefaultsProvider : IStakhanoviseSetupDefaultsProvider
	{
		public const string DefaultConfigFileName = "appsettings.json";

		public const string DefaultConfigSectionName = "Lvd.Stakhanovise.Net.Config";

		private readonly string mBasePath;

		private readonly string mConfigFileName;

		private readonly string mConfigSectionName;

		private readonly IStakhanoviseSetupDefaultsProvider mFallbackDefaultsProvider;

		private readonly string [] mImports;

		private readonly List<string> mAdditionalConfigFileNames = new List<string>();

		public NetCoreConfigurationStakhanoviseDefaultsProvider()
			: this( new ReasonableStakhanoviseDefaultsProvider(), null )
		{
			return;
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider( string [] additionalConfigFileNames )
			: this( new ReasonableStakhanoviseDefaultsProvider(), additionalConfigFileNames )
		{
			return;
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider( IStakhanoviseSetupDefaultsProvider fallbackDefaultsProvider )
			: this( fallbackDefaultsProvider, null )
		{
			return;
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider( IStakhanoviseSetupDefaultsProvider fallbackDefaultsProvider,
			string [] additionalConfigFileNames )
			: this( Directory.GetCurrentDirectory(),
					DefaultConfigFileName,
					DefaultConfigSectionName,
					fallbackDefaultsProvider,
					additionalConfigFileNames )
		{
			return;
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider( string basePath,
			string configFileName,
			string configSectionName )
			: this( basePath,
					configFileName,
					configSectionName,
					new ReasonableStakhanoviseDefaultsProvider(),
					null )
		{

			return;
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider( string basePath,
			string configFileName,
			string configSectionName,
			IStakhanoviseSetupDefaultsProvider fallbackDefaultsProvider,
			string [] additionalConfigFileNames )
		{
			if ( string.IsNullOrEmpty( basePath ) )
				throw new ArgumentNullException( nameof( basePath ) );
			if ( string.IsNullOrEmpty( configFileName ) )
				throw new ArgumentNullException( nameof( configFileName ) );
			if ( string.IsNullOrEmpty( configSectionName ) )
				throw new ArgumentNullException( nameof( configSectionName ) );
			if ( fallbackDefaultsProvider == null )
				throw new ArgumentNullException( nameof( fallbackDefaultsProvider ) );

			mBasePath = basePath;
			mConfigFileName = configFileName;
			mConfigSectionName = configSectionName;
			mFallbackDefaultsProvider = fallbackDefaultsProvider;

			mImports = new string []
			{
				"System",
				"System.Linq",
				"LVD.Stakhanovise.NET",
				"LVD.Stakhanovise.NET.Model",
				"LVD.Stakhanovise.NET.Queue"
			};

			if ( additionalConfigFileNames != null
				&& additionalConfigFileNames.Length > 0 )
			{
				foreach ( string additionalConfigFileName in additionalConfigFileNames )
					AddConfigFileName( additionalConfigFileName );
			}
		}

		public NetCoreConfigurationStakhanoviseDefaultsProvider AddConfigFileName( string configFileName )
		{
			if ( !string.IsNullOrEmpty( configFileName )
				&& !mAdditionalConfigFileNames.Any( f => string.Equals( f, configFileName ) ) )
				mAdditionalConfigFileNames.Add( configFileName );

			return this;
		}

		public StakhanoviseSetupDefaults GetDefaults()
		{
			IConfiguration config = GetConfig();

			IConfigurationSection section = config
				.GetSection( mConfigSectionName );

			StakhanoviseSetupDefaults defaults = mFallbackDefaultsProvider
				.GetDefaults();

			if ( section != null )
			{
				StakhanoviseSetupDefaultsConfig defaultsConfig = section.Get<StakhanoviseSetupDefaultsConfig>();
				if ( defaultsConfig != null )
				{
					defaults = MergeDefaultsFromConfig( defaults,
						defaultsConfig,
						config );
				}
			}

			return defaults;
		}

		private ScriptOptions ConstructParseOptions()
		{
			ScriptOptions parseOptions = ScriptOptions.Default
				.AddReferences( typeof( object ).GetTypeInfo().Assembly )
				.AddReferences( typeof( IQueuedTaskToken ).GetTypeInfo().Assembly )
				.AddReferences( typeof( System.Linq.Enumerable ).GetTypeInfo().Assembly );

			foreach ( string import in mImports )
				parseOptions = parseOptions.AddImports( import );

			return parseOptions;
		}

		private StakhanoviseSetupDefaults MergeDefaultsFromConfig( StakhanoviseSetupDefaults targetDefaults,
			StakhanoviseSetupDefaultsConfig defaultsConfig,
			IConfiguration config )
		{
			ScriptOptions parseOptions = ConstructParseOptions();

			Assembly [] executorAssemblies = ParseExecutorAssembliesFromConfig( defaultsConfig );
			if ( executorAssemblies != null )
				targetDefaults.ExecutorAssemblies = executorAssemblies;

			Func<IQueuedTaskToken, long> calculateDelayTicksTaskAfterFailureFn =
				CompileCalculateDelayTicksTaskAfterFailureFnFromConfig( defaultsConfig, parseOptions );

			if ( calculateDelayTicksTaskAfterFailureFn != null )
				targetDefaults.CalculateDelayMillisecondsTaskAfterFailure = calculateDelayTicksTaskAfterFailureFn;

			Func<IQueuedTask, Exception, bool> isTaskErrorRecoverableFn =
				CompileIsTaskErrorRecoverableFnFromConfig( defaultsConfig, parseOptions );

			if ( isTaskErrorRecoverableFn != null )
				targetDefaults.IsTaskErrorRecoverable = isTaskErrorRecoverableFn;

			if ( !string.IsNullOrWhiteSpace( defaultsConfig.ConnectionStringName ) )
				targetDefaults.ConnectionString = config.GetConnectionString( defaultsConfig.ConnectionStringName );

			QueuedTaskMapping mappingFromConfig =
				GetQueudTaskMappingFromDefaultsConfig( defaultsConfig );

			targetDefaults.Mapping = MergeMappingFromConfig( targetDefaults.Mapping,
				mappingFromConfig );

			if ( defaultsConfig.WorkerCount > 0 )
				targetDefaults.WorkerCount = defaultsConfig.WorkerCount;

			if ( defaultsConfig.FaultErrorThresholdCount > 0 )
				targetDefaults.FaultErrorThresholdCount = defaultsConfig.FaultErrorThresholdCount;

			if ( defaultsConfig.AppMetricsCollectionIntervalMilliseconds > 0 )
				targetDefaults.AppMetricsCollectionIntervalMilliseconds = defaultsConfig.AppMetricsCollectionIntervalMilliseconds;

			if ( defaultsConfig.AppMetricsMonitoringEnabled.HasValue )
				targetDefaults.AppMetricsMonitoringEnabled = defaultsConfig.AppMetricsMonitoringEnabled.Value;

			if ( defaultsConfig.SetupBuiltInDbAsssets.HasValue )
				targetDefaults.SetupBuiltInDbAsssets = defaultsConfig.SetupBuiltInDbAsssets.Value;

			return targetDefaults;
		}

		private QueuedTaskMapping GetQueudTaskMappingFromDefaultsConfig( StakhanoviseSetupDefaultsConfig defaultsConfig )
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();

			if ( defaultsConfig.Mapping != null )
			{
				mapping = new QueuedTaskMapping();
				mapping.DequeueFunctionName = defaultsConfig.Mapping.DequeueFunctionName;
				mapping.ExecutionTimeStatsTableName = defaultsConfig.Mapping.ExecutionTimeStatsTableName;
				mapping.MetricsTableName = defaultsConfig.Mapping.MetricsTableName;
				mapping.NewTaskNotificationChannelName = defaultsConfig.Mapping.NewTaskNotificationChannelName;
				mapping.QueueTableName = defaultsConfig.Mapping.QueueTableName;
				mapping.ResultsQueueTableName = defaultsConfig.Mapping.ResultsQueueTableName;
			}

			return mapping;
		}

		private QueuedTaskMapping MergeMappingFromConfig( QueuedTaskMapping targeMapping, QueuedTaskMapping mappingFromConfig )
		{
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.DequeueFunctionName ) )
				targeMapping.DequeueFunctionName = mappingFromConfig.DequeueFunctionName;
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.ExecutionTimeStatsTableName ) )
				targeMapping.ExecutionTimeStatsTableName = mappingFromConfig.ExecutionTimeStatsTableName;
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.MetricsTableName ) )
				targeMapping.MetricsTableName = mappingFromConfig.MetricsTableName;
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.NewTaskNotificationChannelName ) )
				targeMapping.NewTaskNotificationChannelName = mappingFromConfig.NewTaskNotificationChannelName;
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.QueueTableName ) )
				targeMapping.QueueTableName = mappingFromConfig.QueueTableName;
			if ( !string.IsNullOrWhiteSpace( mappingFromConfig.ResultsQueueTableName ) )
				targeMapping.ResultsQueueTableName = mappingFromConfig.ResultsQueueTableName;

			return targeMapping;
		}

		private Func<IQueuedTaskToken, long> CompileCalculateDelayTicksTaskAfterFailureFnFromConfig( StakhanoviseSetupDefaultsConfig defaultsConfig,
			ScriptOptions parseOptions )
		{
			if ( !string.IsNullOrEmpty( defaultsConfig.CalculateDelayTicksTaskAfterFailure ) )
			{
				return CSharpScript.EvaluateAsync<Func<IQueuedTaskToken, long>>( defaultsConfig.CalculateDelayTicksTaskAfterFailure,
						options: parseOptions )
					.Result;
			}
			else
				return null;
		}

		private Func<IQueuedTask, Exception, bool> CompileIsTaskErrorRecoverableFnFromConfig( StakhanoviseSetupDefaultsConfig defaultsConfig,
			ScriptOptions parseOptions )
		{
			if ( !string.IsNullOrEmpty( defaultsConfig.IsTaskErrorRecoverable ) )
			{
				return CSharpScript.EvaluateAsync<Func<IQueuedTask, Exception, bool>>( defaultsConfig.IsTaskErrorRecoverable,
						options: parseOptions )
					.Result;
			}
			else
				return null;
		}

		private Assembly [] ParseExecutorAssembliesFromConfig( StakhanoviseSetupDefaultsConfig defaultsConfig )
		{
			Assembly [] assemblies = null;

			if ( defaultsConfig.ExecutorAssemblies != null && defaultsConfig.ExecutorAssemblies.Count > 0 )
			{
				assemblies = new Assembly [ defaultsConfig.ExecutorAssemblies.Count ];
				for ( int i = 0; i < defaultsConfig.ExecutorAssemblies.Count; i++ )
					assemblies [ i ] = Assembly.LoadFrom( defaultsConfig.ExecutorAssemblies [ i ] );
			}

			return assemblies;
		}

		private IConfiguration GetConfig()
		{
			IConfigurationBuilder builder = new ConfigurationBuilder()
				.SetBasePath( mBasePath )
				.AddJsonFile( mConfigFileName, 
					optional: false, 
					reloadOnChange: false );

			foreach ( string additionalConfigFileName in mAdditionalConfigFileNames )
			{
				builder.AddJsonFile( additionalConfigFileName, 
					optional: true, 
					reloadOnChange: false );
			}

			return builder
				.Build();
		}
	}
}
