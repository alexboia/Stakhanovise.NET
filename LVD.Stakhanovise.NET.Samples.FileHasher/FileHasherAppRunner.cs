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
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor;
using LVD.Stakhanovise.NET.Samples.FileHasher.Setup;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher
{
	public class FileHasherAppRunner
	{
		private IConfiguration mConfiguration;

		private FileHasherAppConfig mAppConfiguration;

		private string mConnectionString;

		private FileHasherAppAssetsSetup mAppAssetsSetup;

		private ISourceFileGenerator mSourceFileGenerator;

		private ISourceFileRepository mSourceFileRepository;

		private IProcessingWatcher mProcessingWatcher;

		private ISourceFileProcessor mSourceFileProcessor;

		private IFileHashRepository mFileHashRepository;

		public FileHasherAppRunner( string configurationFileName, string appConfigurationSectionName )
		{
			mConfiguration = ProcessConfiguration( configurationFileName );
			mAppConfiguration = GetAppConfiguration( mConfiguration, appConfigurationSectionName );
			mConnectionString = mConfiguration.GetConnectionString( mAppConfiguration.ProducerConnectionStringName );
			mAppAssetsSetup = new FileHasherAppAssetsSetup( mConnectionString );
		}

		private IConfiguration ProcessConfiguration( string configurationFileName ) => new ConfigurationBuilder()
			.SetBasePath( Directory.GetCurrentDirectory() )
			.AddJsonFile( configurationFileName, optional: false, reloadOnChange: false )
			.Build();

		private FileHasherAppConfig GetAppConfiguration( IConfiguration configuration, string appConfigurationSectionName )
		{
			IConfigurationSection appConfigurationSection = configuration
				.GetSection( appConfigurationSectionName );

			return appConfigurationSection
				.Get<FileHasherAppConfig>();
		}

		public async Task RunAsync()
		{
			await SetupAssetsAsync();
			await GenerateFilesToHashAsync();

			SetupProcessing();
			await ProcessFilesAsync();

			DisplayShutdownBanner();
		}

		private async Task GenerateFilesToHashAsync()
		{
			Console.WriteLine( "Generating files to hash..." );

			mSourceFileGenerator = CreateSourceFileGenerator();

			await mSourceFileGenerator
				.CleanupSourceFilesAsync( mAppConfiguration );

			mSourceFileRepository = await mSourceFileGenerator
				.GenerateSourceFilesAsync( mAppConfiguration );

			Console.WriteLine( "Generated {0} files to hash.",
				mSourceFileRepository.TotalFileCount );
		}

		private void SetupProcessing()
		{
			mProcessingWatcher = CreateProcessingWatcher( mSourceFileRepository );
			mFileHashRepository = CreatFileHashRepository();
			mSourceFileProcessor = CreateSourceFileProcessor();
		}

		private async Task ProcessFilesAsync()
		{
			await mSourceFileProcessor.StartProcesingFilesAsync( mSourceFileRepository,
				mFileHashRepository,
				mProcessingWatcher );

			await mProcessingWatcher
				.WaitForCompletionAsync();

			Console.WriteLine( "Processing files completed. Number of hashed files: {0}. Shutting down...",
				mFileHashRepository.TotalHashCount );

			await mSourceFileProcessor
				.StopProcessingFilesAsync();
		}

		private void DisplayShutdownBanner()
		{
			Console.WriteLine( "Successfully shut down. Press any key to continue..." );
			Console.ReadKey();
		}

		private async Task<FileHasherAppRunner> SetupAssetsAsync()
		{
			await mAppAssetsSetup.SetupAsync();
			return this;
		}

		private ISourceFileGenerator CreateSourceFileGenerator()
		{
			return new BogusSourceFileGenerator();
		}

		private TaskQueueOptions GetTaskQueueProducerOptions()
		{
			return new TaskQueueOptions( new ConnectionOptions( mConnectionString ),
				QueuedTaskMapping.Default );
		}

		private IProcessingWatcher CreateProcessingWatcher( ISourceFileRepository sourceFileRepository )
		{
			return new DefaultProcessingWatcher( sourceFileRepository );
		}

		private IFileHashRepository CreatFileHashRepository()
		{
			return new InMemoryFileHashRepository();
		}

		private ISourceFileProcessor CreateSourceFileProcessor()
		{
			return new StakhanoviseSourceFileProcessor( CreateSourceFileScheduler() );
		}

		private ISourceFileScheduler CreateSourceFileScheduler()
		{
			return new AllAtOnceSourceFileScheduler( CreateTaskQueueProducer() );
		}

		private ITaskQueueProducer CreateTaskQueueProducer()
		{
			return new PostgreSqlTaskQueueProducer( GetTaskQueueProducerOptions(),
				new UtcNowTimestampProvider() );
		}
	}
}
