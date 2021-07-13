using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class FileHashInfo
	{
		public FileHashInfo( FileHandle fileHandle, byte[] hash )
		{
			FileHandle = fileHandle;
			Hash = hash;
		}

		public FileHandle FileHandle { get; private set; }

		public byte[] Hash { get; private set; }
	}
}
