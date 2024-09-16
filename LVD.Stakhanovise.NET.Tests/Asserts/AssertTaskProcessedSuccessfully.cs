using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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
			ClassicAssert.NotNull( processingResult );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken.DequeuedTask );
			ClassicAssert.NotNull( processingResult.QueuedTaskToken.LastQueuedTaskResult );

			ClassicAssert.AreEqual( mTaskToken.DequeuedTask,
				processingResult.QueuedTaskToken.DequeuedTask );

			ClassicAssert.NotNull( processingResult.ExecutionResult );
			ClassicAssert.IsTrue( processingResult.ExecutionResult.HasResult );

			ClassicAssert.IsTrue( processingResult.ExecutedSuccessfully );
			ClassicAssert.IsFalse( processingResult.ExecutionCancelled );
			ClassicAssert.IsFalse( processingResult.ExecutionFailed );
			ClassicAssert.Greater( processingResult.ProcessingTimeMilliseconds, 0 );

			ClassicAssert.IsNull( processingResult.Error );
		}
	}
}
