using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertTaskProcessedSuccessfully
	{
		private readonly QueuedTaskToken mTaskToken;

		private AssertTaskProcessedSuccessfully( QueuedTaskToken taskToken )
		{
			mTaskToken = taskToken
				?? throw new ArgumentNullException( nameof( taskToken ) );
		}

		public static AssertTaskProcessedSuccessfully For( QueuedTaskToken taskToken )
		{
			return new AssertTaskProcessedSuccessfully( taskToken );
		}

		public void Check( TaskProcessingResult processingResult )
		{
			Assert.NotNull( processingResult );
			Assert.NotNull( processingResult.QueuedTaskToken );
			Assert.NotNull( processingResult.QueuedTaskToken.DequeuedTask );
			Assert.NotNull( processingResult.QueuedTaskToken.LastQueuedTaskResult );

			Assert.AreEqual( mTaskToken.DequeuedTask,
				processingResult.QueuedTaskToken.DequeuedTask );

			Assert.NotNull( processingResult.ExecutionResult );
			Assert.IsTrue( processingResult.ExecutionResult.HasResult );

			Assert.IsTrue( processingResult.ExecutedSuccessfully );
			Assert.IsFalse( processingResult.ExecutionCancelled );
			Assert.IsFalse( processingResult.ExecutionFailed );
			Assert.Greater( processingResult.ProcessingTimeMilliseconds, 0 );

			Assert.IsNull( processingResult.Error );
		}
	}
}
