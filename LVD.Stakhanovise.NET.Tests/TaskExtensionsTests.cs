using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Helpers;
using System.Threading.Tasks;
using Bogus;
using System.Threading;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class TaskExtensionsTests
	{
		[Test]
		public async Task Test_CanInvokeCleanup_TaskWithSuccessResult ()
		{
			bool cleanupCalled = true;
			int expectedResult = new Faker().Random.Int();

			int result = await Task.Run( async () =>
			{
				await Task.Delay( 500 );
				return expectedResult;
			} ).WithCleanup( prev => cleanupCalled = true );

			Assert.AreEqual( expectedResult, result );
			Assert.IsTrue( cleanupCalled );
		}

		[Test]
		public void Test_CanInvokeCleanup_TaskFailedWithException ()
		{
			Faker faker = null;
			bool cleanupCalled = false;

			Assert.ThrowsAsync<NullReferenceException>( async () => await Task
				.Run( () => faker.Random.Int() )
				.WithCleanup( prev => cleanupCalled = true ) );

			Assert.IsTrue( cleanupCalled );
		}

		[Test]
		public void Test_CanInvokeCleanup_CancelledTask ()
		{
			Faker faker = new Faker();
			CancellationTokenSource cts = new CancellationTokenSource();
			CancellationToken token = cts.Token;
			bool cleanupCalled = false;

			cts.Cancel();

			Assert.CatchAsync<OperationCanceledException>( async () => await Task
				.Run( () =>
				 {
					 token.ThrowIfCancellationRequested();
					 return faker.Random.Int();
				 }, token )
				.WithCleanup( prev => cleanupCalled = true ) );

			Assert.IsTrue( cleanupCalled );
		}
	}
}
