using System;
using System.IO;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor;
using Microsoft.Extensions.Configuration;

namespace LVD.Stakhanovise.NET.Samples.FileHasher
{
	public class Program
	{
		private const string ConfigurationFileName = "appsettings.json";

		private const string ConfigurationSectionName = "Lvd.Stakhanovise.Net.Samples.FileHasher.Config";

		public static void Main( string[] args )
		{
			new Program()
				.RunAsync()
				.Wait();
		}

		private async Task RunAsync()
		{
			FileHasherAppConfig appConfig =
				ReadAppConfig();

			ISourceFileGenerator fileGenerator =
				CreateSourceFileGenerator();
			ISourceFileRepository sourceFileRepository = await fileGenerator
				.GenerateSourceFilesAsync( appConfig );

			ISourceFileScheduler scheduler =
				CreateSourceFileScheduler();

			scheduler.ScheduleFiles( sourceFileRepository );

			IFileHashRepository fileHashRepository =
				CreatFileHashRepository();

			ISourceFileProcessor processor =
				CreateSourceFileProcessor();

			await processor.StartProcesingFilesAsync( fileHashRepository );
		}

		private FileHasherAppConfig ReadAppConfig()
		{
			IConfiguration configuration = GetConfiguration();
			IConfigurationSection appConfigurationSection = configuration
				.GetSection( ConfigurationSectionName );

			return appConfigurationSection
				.Get<FileHasherAppConfig>();
		}

		private IConfiguration GetConfiguration() => new ConfigurationBuilder()
			.SetBasePath( Directory.GetCurrentDirectory() )
			.AddJsonFile( ConfigurationFileName, optional: false, reloadOnChange: false )
			.Build();

		private ISourceFileGenerator CreateSourceFileGenerator()
		{
			return new BogusSourceFileGenerator();
		}

		private ISourceFileScheduler CreateSourceFileScheduler()
		{
			throw new NotImplementedException();
		}

		private IFileHashRepository CreatFileHashRepository()
		{
			throw new NotImplementedException();
		}

		private ISourceFileProcessor CreateSourceFileProcessor()
		{
			throw new NotImplementedException();
		}
	}
}
