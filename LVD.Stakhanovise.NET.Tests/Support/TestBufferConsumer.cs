using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestBufferConsumer
	{
		private ITaskBuffer mTaskBuffer;

		List<IQueuedTaskToken> mConsumedTasks = new List<IQueuedTaskToken>();

		private Task mConsumeBufferTask;

		public TestBufferConsumer ( ITaskBuffer taskBuffer )
		{
			mTaskBuffer = taskBuffer;
		}

		public void ConsumeBuffer ()
		{
			mConsumeBufferTask = Task.Run( () =>
			{
				while ( !mTaskBuffer.IsCompleted )
				{
					IQueuedTaskToken queuedTaskToken = mTaskBuffer.TryGetNextTask();
					if ( queuedTaskToken != null )
						mConsumedTasks.Add( queuedTaskToken );
					else
						Task.Delay( 10 ).Wait();
				}
			} );
		}

		public void WaitForBufferToBeConsumed()
		{
			if ( mConsumeBufferTask == null )
				return;

			mConsumeBufferTask.Wait();
		}

		public List<IQueuedTaskToken> ConsumedTasks 
			=> mConsumedTasks;
	}
}
