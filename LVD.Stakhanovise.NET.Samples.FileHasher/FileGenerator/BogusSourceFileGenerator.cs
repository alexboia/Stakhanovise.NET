// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
		private const string CurrentDirectoryPlaceholder = "${current-directory}";

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
			if ( !Directory.Exists( workingDirectory ) )
				Directory.CreateDirectory( workingDirectory );

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
			string workingDirectory = CurrentDirectoryPlaceholder;

			if ( !string.IsNullOrWhiteSpace( appConfig.WorkingDirectory ) )
				workingDirectory = appConfig.WorkingDirectory;

			return workingDirectory.Replace( CurrentDirectoryPlaceholder,
				Directory.GetCurrentDirectory() );
		}

		private string ComputeFilePath( string workingDirectory, Guid fileId )
		{
			return Path.Combine( workingDirectory, $"{fileId.ToString()}.dat" );
		}

		public Task CleanupSourceFilesAsync( FileHasherAppConfig appConfig )
		{
			if ( appConfig == null )
				throw new ArgumentNullException( nameof( appConfig ) );

			return Task.Run( () => CleanupSourceFiles( appConfig ) );
		}

		private void CleanupSourceFiles( FileHasherAppConfig appConfig )
		{
			string workingDirectory = GetMappedWorkingDirectory( appConfig );
			if ( !Directory.Exists( workingDirectory ) )
				return;

			string[] sourceFiles = Directory.GetFiles( workingDirectory, "*.dat" );

			if ( sourceFiles != null && sourceFiles.Length > 0 )
			{
				foreach ( string sourceFile in sourceFiles )
					File.Delete( sourceFile );
			}
		}
	}
}
