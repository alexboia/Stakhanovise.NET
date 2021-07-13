using LVD.Stakhanovise.NET.Samples.FileHasher.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bogus;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public class BogusSourceFileGenerator : ISourceFileGenerator
	{
		public Task<ISourceFileRepository> GenerateSourceFilesAsync( FileHasherAppConfig appConfig )
		{
			if ( appConfig == null )
				throw new ArgumentNullException( nameof( appConfig ) );

			return Task.Run( () => GenerateFiles( appConfig ) );
		}

		private ISourceFileRepository GenerateFiles( FileHasherAppConfig appConfig )
		{
			Faker faker =
				new Faker();

			List<FileHandle> sourceFiles =
				new List<FileHandle>();

			string workingDirectory = GetMappedWorkingDirectory( appConfig );
			int fileCount = faker.GenerateFileCount( appConfig );

			for ( int i = 0; i < fileCount; i++ )
			{
				Guid fileId = Guid.NewGuid();
				string filePath = ComputeFilePath( workingDirectory, fileId );

				byte[] fileContents = faker.GenerateFileContents( appConfig );
				File.WriteAllBytes( filePath, fileContents );

				FileHandle fileHandle = new FileHandle( fileId, filePath );
				sourceFiles.Add( fileHandle );
			}

			return new InMemorySourceFileRepository( sourceFiles );
		}

		private string GetMappedWorkingDirectory( FileHasherAppConfig appConfig )
		{
			string workingDirectory = ".";

			if ( !string.IsNullOrWhiteSpace( appConfig.WorkingDirectory ) )
				workingDirectory = appConfig.WorkingDirectory;

			if ( workingDirectory == "." )
				return Directory.GetCurrentDirectory();
			else if ( workingDirectory == ".." )
				return Directory.GetParent( Directory.GetCurrentDirectory() )?.FullName;
			else
				return workingDirectory;
		}

		private string ComputeFilePath( string workingDirectory, Guid fileId )
		{
			return Path.Combine( workingDirectory, $"{fileId.ToString()}.dat" );
		}
	}
}
