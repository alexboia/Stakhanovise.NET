using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestBufferFiller : IDisposable
	{
		private ITaskBuffer mTaskBuffer;

		public TestBufferFiller( ITaskBuffer taskBuffer )
		{
			mTaskBuffer = taskBuffer
				?? throw new ArgumentNullException( nameof( taskBuffer ) );
		}

		public void FillBuffer()
		{
			int capacity = mTaskBuffer.Capacity;
			for ( int i = 0; i < capacity; i++ )
				mTaskBuffer.TryAddNewTask( GenerateRandomTaskToken() );
		}

		private IQueuedTaskToken GenerateRandomTaskToken()
		{
			return new MockQueuedTaskToken( Guid.NewGuid() );
		}

		public void Dispose()
		{
			mTaskBuffer = null;
		}
	}
}
