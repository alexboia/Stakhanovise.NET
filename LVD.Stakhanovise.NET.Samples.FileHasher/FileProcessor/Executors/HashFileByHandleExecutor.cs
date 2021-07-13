using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor.SeviceModel;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System.Security.Cryptography;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor.Executors
{
	public class HashFileByHandleExecutor : BaseTaskExecutor<HashFileByHandle>
	{
		public override async Task ExecuteAsync( HashFileByHandle payload, ITaskExecutionContext executionContext )
		{
			using ( SHA256 sha256 = SHA256.Create() )
			{
				FileHandle fileHandle = ResolveFileHandle( payload.HandleId );
				byte[] fileContents = await ReadFileAsync( fileHandle );

				FileHashInfo fileHashInfo = ComputeHash( fileHandle, fileContents );

				StoreFileHashAndNotifyCompletion( fileHashInfo );
			}
		}

		private FileHandle ResolveFileHandle( Guid handleId )
		{
			return SourceFileRepository.ResolveFileByHandleId( handleId );
		}

		private async Task<byte[]> ReadFileAsync( FileHandle fileHandle )
		{
			return await File.ReadAllBytesAsync( fileHandle.Path );
		}

		private FileHashInfo ComputeHash( FileHandle fileHandle, byte[] fileContents )
		{
			using ( SHA256 sha256 = SHA256.Create() )
			{
				byte[] fileHash = sha256.ComputeHash( fileContents );
				FileHashInfo fileHashInfo = new FileHashInfo( fileHandle, fileHash );

				return fileHashInfo;
			}
		}

		private void StoreFileHashAndNotifyCompletion( FileHashInfo fileHashInfo )
		{
			FileHashRepository.AddFileHash( fileHashInfo );
			ProcessingWatcher.NotifyFileHashed( fileHashInfo.FileHandle );
		}

		public ISourceFileRepository SourceFileRepository { get; set; }

		public IFileHashRepository FileHashRepository { get; set; }

		public IProcessingWatcher ProcessingWatcher { get; set; }
	}
}
