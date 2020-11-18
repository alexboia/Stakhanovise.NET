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
using System.IO;
using System.Reflection;
using System.Text;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Setup;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Configuration;

namespace LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings
{
	public class NetCoreConfigurationExtensionsStakhanoviseDefaultsProvider : IStakhanoviseSetupDefaultsProvider
	{
		private string mBasePath;

		private string mConfigFileName;

		private string mConfigSectionName;

		private ReasonableStakhanoviseDefaultsProvider mFallbackDefaultsProvider =
			new ReasonableStakhanoviseDefaultsProvider();

		public NetCoreConfigurationExtensionsStakhanoviseDefaultsProvider ()
		{
			mBasePath = Directory.GetCurrentDirectory();
			mConfigFileName = "appsettings.json";
			mConfigSectionName = "Lvd.Stakhanovise.Net.Config";
		}

		public NetCoreConfigurationExtensionsStakhanoviseDefaultsProvider ( string basePath,
			string configFileName,
			string configSectionName )
		{
			if ( string.IsNullOrEmpty( basePath ) )
				throw new ArgumentNullException( nameof( basePath ) );
			if ( string.IsNullOrEmpty( configFileName ) )
				throw new ArgumentNullException( nameof( configFileName ) );
			if ( string.IsNullOrEmpty( configSectionName ) )
				throw new ArgumentNullException( nameof( configSectionName ) );

			mBasePath = basePath;
			mConfigFileName = configFileName;
			mConfigSectionName = configSectionName;
		}

		public StakhanoviseSetupDefaults GetDefaults ()
		{
			IConfiguration config = GetConfig();

			IConfigurationSection section = config
				.GetSection( mConfigSectionName );

			StakhanoviseSetupDefaults defaults = mFallbackDefaultsProvider
				.GetDefaults();

			StakhanoviseSetupDefaultsConfig defaultsConfig = null;

			if ( section != null )
			{
				defaultsConfig = section.Get<StakhanoviseSetupDefaultsConfig>();
				if ( defaultsConfig != null )
				{
					ScriptOptions parseOptions = ScriptOptions.Default
						.AddReferences( typeof( object ).GetTypeInfo().Assembly )
						.AddReferences( typeof( IQueuedTaskToken ).GetTypeInfo().Assembly )
						.AddReferences( typeof( System.Linq.Enumerable ).GetTypeInfo().Assembly )
						.AddImports( "System" )
						.AddImports( "System.Linq" )
						.AddImports( "LVD.Stakhanovise.NET" )
						.AddImports( "LVD.Stakhanovise.NET.Model" )
						.AddImports( "LVD.Stakhanovise.NET.Queue" );

					Assembly[] executorAssemblies = ParseExecutorAssembliesFromConfig( defaultsConfig );
					if ( executorAssemblies != null )
						defaults.ExecutorAssemblies = executorAssemblies;

					Func<IQueuedTaskToken, long> calculateDelayTicksTaskAfterFailureFn =
						CompileCalculateDelayTicksTaskAfterFailureFnFromConfig( defaultsConfig, parseOptions );

					if ( calculateDelayTicksTaskAfterFailureFn != null )
						defaults.CalculateDelayTicksTaskAfterFailure = calculateDelayTicksTaskAfterFailureFn;

					Func<IQueuedTask, Exception, bool> isTaskErrorRecoverableFn =
						CompileIsTaskErrorRecoverableFnFromConfig( defaultsConfig, parseOptions );

					if ( isTaskErrorRecoverableFn != null )
						defaults.IsTaskErrorRecoverable = isTaskErrorRecoverableFn;

					QueuedTaskMapping mappingFromConfig = GetQueudTaskMappingFromConfig( defaultsConfig );
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.DequeueFunctionName ) )
						defaults.Mapping.DequeueFunctionName = mappingFromConfig.DequeueFunctionName;
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.ExecutionTimeStatsTableName ) )
						defaults.Mapping.ExecutionTimeStatsTableName = mappingFromConfig.ExecutionTimeStatsTableName;
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.MetricsTableName ) )
						defaults.Mapping.MetricsTableName = mappingFromConfig.MetricsTableName;
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.NewTaskNotificationChannelName ) )
						defaults.Mapping.NewTaskNotificationChannelName = mappingFromConfig.NewTaskNotificationChannelName;
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.QueueTableName ) )
						defaults.Mapping.QueueTableName = mappingFromConfig.QueueTableName;
					if ( !string.IsNullOrWhiteSpace( mappingFromConfig.ResultsQueueTableName ) )
						defaults.Mapping.ResultsQueueTableName = mappingFromConfig.ResultsQueueTableName;

					defaults.WorkerCount = defaultsConfig.WorkerCount;
					defaults.FaultErrorThresholdCount = defaultsConfig.FaultErrorThresholdCount;
					defaults.AppMetricsCollectionIntervalMilliseconds = defaultsConfig.AppMetricsCollectionIntervalMilliseconds;
					defaults.AppMetricsMonitoringEnabled = defaultsConfig.AppMetricsMonitoringEnabled;
					defaults.SetupBuiltInDbAsssets = defaultsConfig.SetupBuiltInDbAsssets;

					if ( !string.IsNullOrWhiteSpace( defaultsConfig.ConnectionString ) )
						defaults.ConnectionString = config.GetConnectionString( defaultsConfig.ConnectionString );
				}
			}

			return defaults;
		}

		private QueuedTaskMapping GetQueudTaskMappingFromConfig ( StakhanoviseSetupDefaultsConfig config )
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();

			if ( config.Mapping != null )
			{
				mapping = new QueuedTaskMapping();
				mapping.DequeueFunctionName = config.Mapping.DequeueFunctionName;
				mapping.ExecutionTimeStatsTableName = config.Mapping.ExecutionTimeStatsTableName;
				mapping.MetricsTableName = config.Mapping.MetricsTableName;
				mapping.NewTaskNotificationChannelName = config.Mapping.NewTaskNotificationChannelName;
				mapping.QueueTableName = config.Mapping.QueueTableName;
				mapping.ResultsQueueTableName = config.Mapping.ResultsQueueTableName;
			}

			return mapping;
		}

		private Func<IQueuedTaskToken, long> CompileCalculateDelayTicksTaskAfterFailureFnFromConfig ( StakhanoviseSetupDefaultsConfig config,
			ScriptOptions parseOptions )
		{
			if ( !string.IsNullOrEmpty( config.CalculateDelayTicksTaskAfterFailure ) )
			{
				return CSharpScript.EvaluateAsync<Func<IQueuedTaskToken, long>>( config.CalculateDelayTicksTaskAfterFailure,
						options: parseOptions )
					.Result;
			}
			else
				return null;
		}

		private Func<IQueuedTask, Exception, bool> CompileIsTaskErrorRecoverableFnFromConfig ( StakhanoviseSetupDefaultsConfig config,
			ScriptOptions parseOptions )
		{
			if ( !string.IsNullOrEmpty( config.IsTaskErrorRecoverable ) )
			{
				return CSharpScript.EvaluateAsync<Func<IQueuedTask, Exception, bool>>( config.IsTaskErrorRecoverable,
						options: parseOptions )
					.Result;
			}
			else
				return null;
		}

		private Assembly[] ParseExecutorAssembliesFromConfig ( StakhanoviseSetupDefaultsConfig config )
		{
			Assembly[] assemblies = null;

			if ( config.ExecutorAssemblies != null && config.ExecutorAssemblies.Count > 0 )
			{
				assemblies = new Assembly[ config.ExecutorAssemblies.Count ];
				for ( int i = 0; i < config.ExecutorAssemblies.Count; i++ )
					assemblies[ i ] = Assembly.LoadFrom( config.ExecutorAssemblies[ i ] );
			}

			return assemblies;
		}

		private IConfiguration GetConfig () => new ConfigurationBuilder()
			.SetBasePath( mBasePath )
			.AddJsonFile( mConfigFileName, optional: false, reloadOnChange: false )
			.Build();
	}
}
