using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
			ClassicAssert.NotNull( processingResult );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken.DequeuedTask );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken.LastQueuedTaskResult );

			ClassicAssert.AreEqual( mTaskToken.DequeuedTask,
				processingResult.QueuedTaskToken.DequeuedTask );

			ClassicAssert.NotNull( processingResult.ExecutionResult );
			ClassicAssert.IsTrue( processingResult.ExecutionResult.HasResult );

			ClassicAssert.IsFalse( processingResult.ExecutedSuccessfully );
			ClassicAssert.IsFalse( processingResult.ExecutionCancelled );
			ClassicAssert.IsTrue( processingResult.ExecutionFailed );
			ClassicAssert.Greater( processingResult.ProcessingTimeMilliseconds, 0 );

			ClassicAssert.NotNull( processingResult.Error );
		}
	}
}
