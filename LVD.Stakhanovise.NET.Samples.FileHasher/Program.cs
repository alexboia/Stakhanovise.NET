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
using System;
using System.IO;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Options;
using LVD.Stakhanovise.NET.Model;
using Microsoft.Extensions.Configuration;
using LVD.Stakhanovise.NET.Samples.FileHasher.Setup;

namespace LVD.Stakhanovise.NET.Samples.FileHasher
{
	public class Program
	{
		private const string ConfigurationFileName = "appsettings.json";

		private const string ConfigurationSectionName = "Lvd.Stakhanovise.Net.Samples.FileHasher.Config";

		private IConfiguration mConfiguration;

		private FileHasherAppConfig mAppConfiguration;

		private string mConnectionString;

		private FileHasherAppSetup mAppSetup;

		public static void Main( string[] args )
		{
			new Program()
				.RunAsync()
				.Wait();
		}

		public Program()
		{
			mConfiguration = ProcessConfiguration();
			mAppConfiguration = GetAppConfiguration( mConfiguration );
			mConnectionString = mConfiguration.GetConnectionString( mAppConfiguration.ProducerConnectionStringName );
			mAppSetup = new FileHasherAppSetup( mConnectionString );
		}

		private IConfiguration ProcessConfiguration() => new ConfigurationBuilder()
			.SetBasePath( Directory.GetCurrentDirectory() )
			.AddJsonFile( ConfigurationFileName, optional: false, reloadOnChange: false )
			.Build();

		private FileHasherAppConfig GetAppConfiguration( IConfiguration configuration )
		{
			IConfigurationSection appConfigurationSection = configuration
				.GetSection( ConfigurationSectionName );

			return appConfigurationSection
				.Get<FileHasherAppConfig>();
		}

		private async Task RunAsync()
		{
			await SetupAsync();

			Console.WriteLine( "Generating files to hash..." );

			ISourceFileGenerator fileGenerator =
				CreateSourceFileGenerator();

			await fileGenerator.CleanupSourceFilesAsync( mAppConfiguration );

			ISourceFileRepository sourceFileRepository = await fileGenerator
				.GenerateSourceFilesAsync( mAppConfiguration );

			Console.WriteLine( "Generated {0} files to hash.",
				sourceFileRepository.TotalFileCount );

			IProcessingWatcher processingWatcher =
				CreateProcessingWatcher( sourceFileRepository );

			ISourceFileScheduler sourceFileScheduler =
				CreateSourceFileScheduler();

			IFileHashRepository fileHashRepository =
				CreatFileHashRepository();

			ISourceFileProcessor processor =
				CreateSourceFileProcessor( sourceFileScheduler );

			Console.WriteLine( "Begin processing files..." );

			await processor.StartProcesingFilesAsync( sourceFileRepository,
				fileHashRepository,
				processingWatcher );

			await processingWatcher.WaitForCompletionAsync();

			Console.WriteLine( "Processing files completed. Number of hashed files: {0}. Shutting down...",
				fileHashRepository.TotalHashCount );

			await processor.StopProcessingFilesAsync();

			Console.WriteLine( "Successfully shut down. Press any key to continue..." );
			Console.ReadKey();
		}

		private async Task<Program> SetupAsync()
		{
			await mAppSetup.SetupAsync();
			return this;
		}

		private ISourceFileGenerator CreateSourceFileGenerator()
		{
			return new BogusSourceFileGenerator();
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

		private ISourceFileProcessor CreateSourceFileProcessor( ISourceFileScheduler sourceFileScheduler )
		{
			return new StakhanoviseSourceFileProcessor( sourceFileScheduler );
		}
	}
}
