using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestTaskQueueAbstractTimeProvider : ITaskQueueAbstractTimeProvider
	{
		private Func<AbstractTimestamp> mCurrentTimeProvider;

		public TestTaskQueueAbstractTimeProvider ( Func<AbstractTimestamp> currentTimeProvider )
		{
			mCurrentTimeProvider = currentTimeProvider
				?? throw new ArgumentNullException( nameof( currentTimeProvider ) );
		}

		public Task<long> ComputeAbsoluteTimeTicksAsync ( long timeTicksToAdd )
		{
			return Task.FromResult( GetCurrentTimeTicks() 
				+ timeTicksToAdd );
		}

		public Task<AbstractTimestamp> GetCurrentTimeAsync ()
		{
			return Task.FromResult( GetCurrentTime() );
		}

		private AbstractTimestamp GetCurrentTime ()
		{
			return mCurrentTimeProvider.Invoke();
		}

		private long GetCurrentTimeTicks ()
		{
			return GetCurrentTime().Ticks;
		}
	}
}
