using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Tests.Asserts
{
	public class AssertTaskProcessingCancelled
	{
		private readonly QueuedTaskToken mTaskToken;

		private AssertTaskProcessingCancelled( QueuedTaskToken taskToken )
		{
			mTaskToken = taskToken
				?? throw new ArgumentNullException( nameof( taskToken ) );
		}

		public static AssertTaskProcessingCancelled For( QueuedTaskToken taskToken )
		{
			return new AssertTaskProcessingCancelled( taskToken );
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
			ClassicAssert.IsTrue( processingResult.ExecutionCancelled );
			ClassicAssert.IsFalse( processingResult.ExecutionFailed );
			ClassicAssert.Greater( processingResult.ProcessingTimeMilliseconds, 0 );

			ClassicAssert.IsNull( processingResult.Error );
		}
	}
}
