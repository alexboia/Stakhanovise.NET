using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator
{
	public class InMemorySourceFileRepository : ISourceFileRepository
	{
		private ConcurrentDictionary<Guid, FileHandle> mFileHandleDictionary;

		public InMemorySourceFileRepository( IEnumerable<FileHandle> fileHandles )
		{
			mFileHandleDictionary = new ConcurrentDictionary<Guid, FileHandle>( CreateDictionarySource( fileHandles ) );
		}

		private IEnumerable<KeyValuePair<Guid, FileHandle>> CreateDictionarySource( IEnumerable<FileHandle> fileHandles )
		{
			return fileHandles.Select( fh => new KeyValuePair<Guid, FileHandle>( fh.Id, fh ) );
		}

		public FileHandle ResolveFileByHandleId( Guid handleId )
		{
			FileHandle result;

			if ( !mFileHandleDictionary.TryGetValue( handleId, out result ) )
				result = null;

			return result;
		}

		public IEnumerable<Guid> AllFileHandleIds => mFileHandleDictionary.Keys;

		public int TotalFileCount => mFileHandleDictionary.Count;
	}
}
