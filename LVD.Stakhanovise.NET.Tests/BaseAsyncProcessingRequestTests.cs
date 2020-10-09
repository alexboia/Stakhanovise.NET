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
		protected void RunTest_CanSetCompleted_SingleThread<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn,
			TResult expectedResult,
			bool expectSame )
		{
			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

			rq.SetCompleted( expectedResult );

			TResult actualResult = tcs.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				tcs.Task.Status );

			if ( !expectSame )
				Assert.AreEqual( expectedResult,
					actualResult );
			else
				Assert.AreSame( expectedResult,
					actualResult );
		}

		protected async Task RunTest_CanSetCompleted_MultiThread<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn,
			bool syncOnCheckpoints,
			int nThreads,
			TResult expectedResult,
			bool expectSame )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();
			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

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

			TResult actualResult = tcs.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				tcs.Task.Status );

			if ( !expectSame )
				Assert.AreEqual( expectedResult,
					actualResult );
			else
				Assert.AreSame( expectedResult,
					actualResult );
		}

		protected void RunTest_CanSetCancelledManually_SingleThread<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn )
		{
			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

			rq.SetCancelled();

			Assert.AreEqual( TaskStatus.Canceled,
				tcs.Task.Status );
		}

		protected async Task RunTest_CanSetCancelledManually_MultiThread<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn,
			bool syncOnCheckpoints,
			int nThreads )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();
			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

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
				tcs.Task.Status );
		}

		protected void RunTest_CanCancelItselfViaTimeout<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn )
		{
			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

			Assert.CatchAsync<TaskCanceledException>( async ()
				=> await tcs.Task );

			Assert.AreEqual( TaskStatus.Canceled,
				tcs.Task.Status );
		}

		protected void RunTest_CanSetFailed_SingleThread<TResult> ( Func<TaskCompletionSource<TResult>, AsyncProcessingRequest<TResult>> rqFn,
			int maxFailCount )
		{
			Exception exc = new Exception( "Sample exception" );

			TaskCompletionSource<TResult> tcs =
				new TaskCompletionSource<TResult>( TaskCreationOptions.RunContinuationsAsynchronously );

			AsyncProcessingRequest<TResult> rq = rqFn
				.Invoke( tcs );

			for ( int i = 0; i < maxFailCount; i++ )
			{
				rq.SetFailed( exc );

				if ( i < maxFailCount - 1 )
				{
					Assert.IsTrue( rq.CanBeRetried );

					Exception actualExc = tcs.Task
						.Exception;

					Assert.AreEqual( TaskStatus.WaitingForActivation,
						tcs.Task.Status );

					Assert.Null( actualExc );
				}
				else
				{
					Assert.IsFalse( rq.CanBeRetried );

					Exception actualExc = tcs.Task
						.Exception;

					actualExc = ( actualExc is AggregateException )
						? ( ( AggregateException )actualExc ).InnerException
						: actualExc;

					Assert.AreEqual( TaskStatus.Faulted,
						tcs.Task.Status );

					Assert.NotNull( actualExc );
					Assert.AreSame( exc, actualExc );
				}
			}
		}
	}
}
