using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public class TestExecutorEventBus<T>
	{
		public EventHandler ExecutorCompleted;

		public static readonly TestExecutorEventBus<T> Instance =
			new TestExecutorEventBus<T>();

		private TestExecutorEventBus ()
		{
			return;
		}

		public void NotifyExecutorCompleted()
		{
			EventHandler executorCompletedEh = ExecutorCompleted;
			if ( executorCompletedEh != null )
				executorCompletedEh( this, EventArgs.Empty );
		}

		public void Reset()
		{
			ExecutorCompleted = null;
		}
	}
}
