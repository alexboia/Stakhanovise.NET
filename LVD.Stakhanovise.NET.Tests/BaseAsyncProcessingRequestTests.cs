using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	abstract public class BaseAsyncProcessingRequestTests
	{
		protected void RunTest_CanSetCompleted_SingleThread<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn,
			TResult expectedResult,
			bool expectSame )
		{
			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			rq.SetCompleted( expectedResult );

			TResult actualResult = rq.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				rq.Task.Status );

			if ( !expectSame )
				Assert.AreEqual( expectedResult,
					actualResult );
			else
				Assert.AreSame( expectedResult,
					actualResult );
		}

		protected async Task RunTest_CanSetCompleted_MultiThread<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn,
			bool syncOnCheckpoints,
			int nThreads,
			TResult expectedResult,
			bool expectSame )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			for ( int i = 0; i < nThreads; i++ )
			{
				allThreads.Add( Task.Run( () =>
				{
					if ( syncOnCheckpoints )
						syncCheckpoint.SignalAndWait();
					rq.SetCompleted( expectedResult );
				} ) );
			}

			await Task.WhenAll( allThreads );

			TResult actualResult = rq.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				rq.Task.Status );

			if ( !expectSame )
				Assert.AreEqual( expectedResult,
					actualResult );
			else
				Assert.AreSame( expectedResult,
					actualResult );
		}

		protected void RunTest_CanSetCancelledManually_SingleThread<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn )
		{
			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			rq.SetCancelled();

			Assert.AreEqual( TaskStatus.Canceled,
				rq.Task.Status );
		}

		protected async Task RunTest_CanSetCancelledManually_MultiThread<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn,
			bool syncOnCheckpoints,
			int nThreads )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			for ( int i = 0; i < nThreads; i++ )
			{
				allThreads.Add( Task.Run( () =>
				{
					if ( syncOnCheckpoints )
						syncCheckpoint.SignalAndWait();
					rq.SetCancelled();
				} ) );
			}

			await Task.WhenAll( allThreads );

			Assert.AreEqual( TaskStatus.Canceled,
				rq.Task.Status );
		}

		protected void RunTest_CanCancelItselfViaTimeout<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn )
		{
			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			Assert.CatchAsync<TimeoutException>( async ()
				=> await rq.Task );

			Assert.AreEqual( TaskStatus.Faulted,
				rq.Task.Status );

			Assert.IsTrue( rq.IsTimedOut );
		}

		protected void RunTest_CanSetFailed_SingleThread<TResult> ( Func<AsyncProcessingRequest<TResult>> rqFn,
			int maxFailCount )
		{
			Exception exc = new Exception( "Sample exception" );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke();

			for ( int i = 0; i < maxFailCount; i++ )
			{
				rq.SetFailed( exc );

				if ( i < maxFailCount - 1 )
				{
					Assert.IsTrue( rq.CanBeRetried );

					Exception actualExc = rq.Task
						.Exception;

					Assert.AreEqual( TaskStatus.WaitingForActivation,
						rq.Task.Status );

					Assert.Null( actualExc );
				}
				else
				{
					Assert.IsFalse( rq.CanBeRetried );

					Exception actualExc = rq.Task
						.Exception;

					actualExc = ( actualExc is AggregateException )
						? ( ( AggregateException )actualExc ).InnerException
						: actualExc;

					Assert.AreEqual( TaskStatus.Faulted,
						rq.Task.Status );

					Assert.NotNull( actualExc );
					Assert.AreSame( exc, actualExc );
				}
			}
		}
	}
}
