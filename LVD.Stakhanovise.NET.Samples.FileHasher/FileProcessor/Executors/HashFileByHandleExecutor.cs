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
