using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using Moq;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{

	[TestFixture]
	public class TaskExecutionContextTests
	{
		[Test]
		public void Test_CanNotifyTaskCompleted ()
		{
			Faker faker = new Faker();

			Mock<IQueuedTaskToken> taskMock =
				new Mock<IQueuedTaskToken>( MockBehavior.Loose );

			using ( CancellationTokenSource cts = new CancellationTokenSource() )
			{
				TaskExecutionContext ctx = new TaskExecutionContext( taskMock.Object, cts.Token );
				ctx.NotifyTaskCompleted();

				Assert.IsTrue( ctx.HasResult );
				Assert.NotNull( ctx.ResultInfo );
				Assert.IsTrue( ctx.ResultInfo.ExecutedSuccessfully );
				Assert.IsFalse( ctx.ResultInfo.ExecutionCancelled );
				Assert.IsFalse( ctx.IsCancellationRequested );
			}
		}

		[Test]
		public void Test_CanNotifyTaskErrored ()
		{
			Faker faker = new Faker();

			Mock<IQueuedTaskToken> taskMock =
				new Mock<IQueuedTaskToken>( MockBehavior.Loose );

			using ( CancellationTokenSource cts = new CancellationTokenSource() )
			{
				Exception exc = faker.System.Exception();
				bool isRecoverable = faker.Random.Bool();

				TaskExecutionContext ctx = new TaskExecutionContext( taskMock.Object, cts.Token );

				ctx.NotifyTaskErrored( new QueuedTaskError( exc ), isRecoverable );

				Assert.IsTrue( ctx.HasResult );
				Assert.NotNull( ctx.ResultInfo );
				Assert.IsFalse( ctx.ResultInfo.ExecutedSuccessfully );
				Assert.IsFalse( ctx.ResultInfo.ExecutionCancelled );

				Assert.NotNull( ctx.ResultInfo.Error );
				Assert.AreEqual( exc.GetType().FullName, ctx.ResultInfo.Error.Type );
				Assert.AreEqual( exc.Message, ctx.ResultInfo.Error.Message );
				Assert.AreEqual( exc.StackTrace, ctx.ResultInfo.Error.StackTrace );

				Assert.AreEqual( isRecoverable, ctx.ResultInfo.IsRecoverable );

				Assert.IsFalse( ctx.IsCancellationRequested );
			}
		}

		[Test]
		public void Test_CanNotifyCancellationObserved ()
		{
			Faker faker = new Faker();

			Mock<IQueuedTaskToken> taskMock =
				new Mock<IQueuedTaskToken>( MockBehavior.Loose );

			using ( CancellationTokenSource cts = new CancellationTokenSource() )
			{
				TaskExecutionContext ctx = new TaskExecutionContext( taskMock.Object, cts.Token );

				ctx.NotifyCancellationObserved();
				Assert.IsTrue( ctx.HasResult );
				Assert.NotNull( ctx.ResultInfo );
				Assert.IsFalse( ctx.ResultInfo.ExecutedSuccessfully );
				Assert.IsTrue( ctx.ResultInfo.ExecutionCancelled );
			}
		}

		[Test]
		public void Test_CanThrowIfCancellationRequested ()
		{
			Faker faker = new Faker();

			Mock<IQueuedTaskToken> taskMock =
				new Mock<IQueuedTaskToken>( MockBehavior.Loose );

			using ( CancellationTokenSource cts = new CancellationTokenSource() )
			{
				TaskExecutionContext ctx = new TaskExecutionContext( taskMock.Object, cts.Token );
				cts.Cancel();
				Assert.Throws<OperationCanceledException>( () => ctx.ThrowIfCancellationRequested() );
			}
		}
	}
}
