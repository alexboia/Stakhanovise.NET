using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertTaskProcessedWithError
	{
		private readonly QueuedTaskToken mTaskToken;

		private AssertTaskProcessedWithError( QueuedTaskToken taskToken )
		{
			mTaskToken = taskToken
				?? throw new ArgumentNullException( nameof( taskToken ) );
		}

		public static AssertTaskProcessedWithError For(QueuedTaskToken taskToken)
		{
			return new AssertTaskProcessedWithError( taskToken );
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

			Assert.IsFalse( processingResult.ExecutedSuccessfully );
			Assert.IsFalse( processingResult.ExecutionCancelled );
			Assert.IsTrue( processingResult.ExecutionFailed );
			Assert.Greater( processingResult.ProcessingTimeMilliseconds, 0 );

			Assert.NotNull( processingResult.Error );
		}
	}
}
