using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.Net.Info.Tests.Asserts
{
	public class AssertCorrectTopOfQueue
	{
		private IQueuedTask mExpectedTopOfQueue;

		private AssertCorrectTopOfQueue( IQueuedTask expectedTopOfQueue )
		{
			mExpectedTopOfQueue = expectedTopOfQueue;
		}

		public static AssertCorrectTopOfQueue WithExpected( IQueuedTask expectedTopOfQueue )
		{
			return new AssertCorrectTopOfQueue( expectedTopOfQueue );
		}

		public void Check( IQueuedTask actualTopOfQueue )
		{
			if ( mExpectedTopOfQueue != null )
			{
				Assert.NotNull( actualTopOfQueue );
				Assert.AreEqual( mExpectedTopOfQueue.Id,
					actualTopOfQueue.Id );
			}
			else
				Assert.IsNull( actualTopOfQueue );			
		}
	}
}
