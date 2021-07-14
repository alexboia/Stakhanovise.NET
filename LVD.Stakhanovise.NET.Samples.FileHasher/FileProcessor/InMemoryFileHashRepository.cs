using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class InMemoryFileHashRepository : IFileHashRepository
	{
		private ConcurrentDictionary<Guid, FileHashInfo> mFileHashDictionary =
			new ConcurrentDictionary<Guid, FileHashInfo>();

		public void AddFileHash( FileHashInfo hashInfo )
		{
			if ( hashInfo == null )
				throw new ArgumentNullException( nameof( hashInfo ) );

			mFileHashDictionary.TryAdd( hashInfo.FileHandle.Id,
				hashInfo );
		}

		public IEnumerable<FileHashInfo> GeneratedHashes
			=> mFileHashDictionary.Values;

		public int TotalHashCount
			=> mFileHashDictionary.Count;
	}
}
