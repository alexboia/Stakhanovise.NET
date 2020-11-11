// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
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
