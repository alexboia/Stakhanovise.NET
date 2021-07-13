using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public interface IFileHashRepository
	{
		void AddFileHash( FileHashInfo hashInfo );

		IEnumerable<FileHashInfo> GeneratedHashes { get; }

		int TotalHashCount { get; }
	}
}
