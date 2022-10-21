using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LVD.Stakhanovise.NET.Model
{
	public class AsyncProcessingRequestBatch<TElement> : IEnumerable<TElement>
	{
		private readonly int mMaxSize;

		private readonly Queue<TElement> mQueuedBatch =
			new Queue<TElement>();

		public AsyncProcessingRequestBatch( int maxSize )
		{
			mMaxSize = maxSize;
		}

		public void FillFrom( BlockingCollection<TElement> requestSource,
			CancellationToken stopToken )
		{
			TElement processItem;

			try
			{
				//Try to dequeue and block if no item is available
				processItem = requestSource.Take( stopToken );
				mQueuedBatch.Enqueue( processItem );
			}
			catch ( OperationCanceledException )
			{
				//We're only using the cancellation token here to exit the blocking .Take(),
				//	but we also need to extract the remainder of the items
				//	if the cancellation token has been signaled;
				//	thus, we won't allow the exception to propagate
			}

			//See if there are other items available
			//	and add them to current batch;
			// If cancellation was requested,
			//	pull all the remaining items
			while ( CanExtractOneMoreItem( stopToken ) && requestSource.TryTake( out processItem ) )
				mQueuedBatch.Enqueue( processItem );
		}

		private bool CanExtractOneMoreItem( CancellationToken stopToken )
		{
			return mQueuedBatch.Count < mMaxSize || stopToken.IsCancellationRequested;
		}

		public TElement Dequeue()
		{
			return mQueuedBatch.Dequeue();
		}

		public void Clear()
		{
			mQueuedBatch.Clear();
		}

		public IEnumerator<TElement> GetEnumerator()
		{
			return mQueuedBatch.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count => mQueuedBatch.Count;
	}
}
