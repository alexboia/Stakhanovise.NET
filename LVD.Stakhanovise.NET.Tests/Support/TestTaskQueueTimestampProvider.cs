using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestTaskQueueTimestampProvider : ITimestampProvider
	{
		private Func<DateTimeOffset> mCurrentTimeProvider;

		public TestTaskQueueTimestampProvider ( Func<DateTimeOffset> currentTimeProvider )
		{
			mCurrentTimeProvider = currentTimeProvider
				?? throw new ArgumentNullException( nameof( currentTimeProvider ) );
		}

		public DateTimeOffset GetNow ()
		{
			return mCurrentTimeProvider.Invoke();
		}
	}
}
