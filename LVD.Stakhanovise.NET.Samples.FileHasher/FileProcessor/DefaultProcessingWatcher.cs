using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class DefaultProcessingWatcher : IProcessingWatcher
	{
		public event EventHandler FileHashAdded;

		private TaskCompletionSource<int> mProcessingCompletionSource;

		private ISourceFileRepository mSourceFileRepository;

		private int mTotalHashedFileCount = 0;

		public DefaultProcessingWatcher( ISourceFileRepository sourceFileRepository )
		{
			mSourceFileRepository = sourceFileRepository
				?? throw new ArgumentNullException( nameof( sourceFileRepository ) );
			mProcessingCompletionSource = new TaskCompletionSource<int>();
		}

		public void NotifyFileHashed( FileHandle fileHandle )
		{
			if ( fileHandle == null )
				throw new ArgumentNullException( nameof( fileHandle ) );

			DispatchFileHashedEvent( fileHandle );
			IncrementTotalHashedFileCount();
			if ( HaveAllFilesBeenHashed() )
				SetProcessingCompleted();
		}

		private void DispatchFileHashedEvent( FileHandle fileHandle )
		{
			EventHandler callback = FileHashAdded;
			if ( callback != null )
				callback( this, EventArgs.Empty );
		}

		private void IncrementTotalHashedFileCount()
		{
			Interlocked.Increment( ref mTotalHashedFileCount );
		}

		private bool HaveAllFilesBeenHashed()
		{
			return mTotalHashedFileCount == mSourceFileRepository.TotalFileCount;
		}

		private void SetProcessingCompleted()
		{
			mProcessingCompletionSource.TrySetResult( mTotalHashedFileCount );
		}

		public Task WaitForCompletionAsync()
		{
			return mProcessingCompletionSource.Task;
		}
	}
}
