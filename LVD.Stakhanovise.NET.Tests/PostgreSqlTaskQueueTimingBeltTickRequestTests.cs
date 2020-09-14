using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgreSqlTaskQueueTimingBeltTickRequestTests
	{
		[Test]
		public void Test_CanSetCompleted_SingleThread ()
		{
			AbstractTimestamp timestamp =
				new AbstractTimestamp( 10, 1000 );

			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: 0,
				maxFailCount: 3 );

			rq.SetCompleted( timestamp );

			AbstractTimestamp actualTimestmap = tcs.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				tcs.Task.Status );

			Assert.NotNull( actualTimestmap );
			Assert.AreSame( timestamp, actualTimestmap );
		}

		[Test]
		[TestCase( true, 2 )]
		[TestCase( true, 5 )]
		[TestCase( true, 10 )]

		[TestCase( false, 2 )]
		[TestCase( false, 5 )]
		[TestCase( false, 10 )]
		public async Task Test_CanSetCompleted_MultiThread ( bool syncOnCheckpoints, int nThreads )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();

			AbstractTimestamp timestamp =
				new AbstractTimestamp( 10, 1000 );

			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: 0,
				maxFailCount: 3 );

			for ( int i = 0; i < nThreads; i++ )
			{
				allThreads.Add( Task.Run( () =>
				 {
					 if ( syncOnCheckpoints )
						 syncCheckpoint.SignalAndWait();
					 rq.SetCompleted( timestamp );
				 } ) );
			}

			await Task.WhenAll( allThreads );

			AbstractTimestamp actualTimestmap = tcs.Task
				.Result;

			Assert.AreEqual( TaskStatus.RanToCompletion,
				tcs.Task.Status );

			Assert.NotNull( actualTimestmap );
			Assert.AreSame( timestamp, actualTimestmap );
		}

		[Test]
		public void Test_CanSetCancelledManually_SingleThread ()
		{
			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: 0,
				maxFailCount: 3 );

			rq.SetCancelled();

			Assert.AreEqual( TaskStatus.Canceled,
				tcs.Task.Status );
		}

		[Test]
		[TestCase( true, 2 )]
		[TestCase( true, 5 )]
		[TestCase( true, 10 )]

		[TestCase( false, 2 )]
		[TestCase( false, 5 )]
		[TestCase( false, 10 )]
		public async Task Test_CanSetCancelledManually_MultiThread ( bool syncOnCheckpoints, int nThreads )
		{
			Barrier syncCheckpoint =
				new Barrier( nThreads );

			List<Task> allThreads =
				new List<Task>();

			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: 0,
				maxFailCount: 3 );

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

		[Test]
		[TestCase( 100 )]
		[TestCase( 500 )]
		[TestCase( 1000 )]
		public void Test_CanCancelItselfViaTimeout ( int timeoutMilliseconds )
		{
			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: timeoutMilliseconds,
				maxFailCount: 3 );

			Assert.CatchAsync<TaskCanceledException>( async ()
				=> await tcs.Task );

			Assert.AreEqual( TaskStatus.Canceled,
				tcs.Task.Status );
		}

		[Test]
		[TestCase( 0 )]
		[TestCase( 3 )]
		[TestCase( 5 )]
		public void Test_CanSetFailed_SingleThread ( int maxFailCount )
		{
			Exception exc = new Exception( "Sample exception" );

			TaskCompletionSource<AbstractTimestamp> tcs =
				new TaskCompletionSource<AbstractTimestamp>( TaskCreationOptions.RunContinuationsAsynchronously );

			PostgreSqlTaskQueueTimingBeltTickRequest rq = new PostgreSqlTaskQueueTimingBeltTickRequest( 1,
				completionToken: tcs,
				timeoutMilliseconds: 0,
				maxFailCount: maxFailCount );

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
