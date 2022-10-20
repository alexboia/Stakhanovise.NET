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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class StateControllerTests
	{
		[Test]
		public async Task Test_CanSwitchStates_DelegateNoExceptions ()
		{
			StateController stateController = new StateController();

			AssertControllerStopped( stateController );

			await stateController.TryRequestStartAsync( async () => { await Task.CompletedTask; } );
			AssertControllerStarted( stateController );

			await stateController.TryRequestStopAsync( async () => { await Task.CompletedTask; } );
			AssertControllerStopped( stateController );
		}

		[Test]
		public async Task Test_CanSwitchStates_TryStart_DelegatesWithExceptions ()
		{
			StateController stateController = new StateController();

			AssertControllerStopped( stateController );

			try
			{
				await stateController.TryRequestStartAsync( async () =>
				{
					await Task.FromException( new InvalidOperationException( "Sample invalid operation exception" ) );
				} );
			}
			catch ( InvalidOperationException )
			{
				Assert.Pass();
			}
			finally
			{
				AssertControllerStopped( stateController );
			}
		}

		[Test]
		public async Task Test_CanSwitchStates_TryStop_DelegatesWithExceptions ()
		{
			StateController stateController = new StateController();

			AssertControllerStopped( stateController );
			await stateController.TryRequestStartAsync( async () => { await Task.CompletedTask; } );

			try
			{
				await stateController.TryRequestStopAsync( async () =>
				{
					await Task.FromException( new InvalidOperationException( "Sample invalid operation exception" ) );
				} );
			}
			catch ( InvalidOperationException )
			{
				Assert.Pass();
			}
			finally
			{
				AssertControllerStopped( stateController );
			}
		}

		[Test]
		[TestCase( 1 )]
		[TestCase( 2 )]
		[TestCase( 5 )]
		[TestCase( 10 )]
		[Repeat( 5 )]
		public async Task Test_CanConcurrentlySafelyChangeState ( int threadNumber )
		{
			int tryRequestStartCount = 0,
				tryRequestStopCount = 0;

			List<Task> tasks = new List<Task>();
			Barrier barrier = new Barrier( threadNumber );
			StateController controller = new StateController();

			for ( int i = 0; i < threadNumber; i++ )
			{
				tasks.Add( Task.Run( async () =>
				{
					barrier.SignalAndWait();
					await controller.TryRequestStartAsync( async () =>
					{
						Interlocked.Increment( ref tryRequestStartCount );
						await Task.CompletedTask;
					} );

					barrier.SignalAndWait();
					await controller.TryRequestStopAsync( async () =>
					{
						Interlocked.Increment( ref tryRequestStopCount );
						await Task.CompletedTask;
					} );
				} ) );
			}

			await Task.WhenAll( tasks );

			Assert.AreEqual( 1, tryRequestStartCount );
			Assert.AreEqual( 1, tryRequestStopCount );
		}

		private void AssertControllerStopped ( StateController stateController )
		{
			Assert.IsFalse( stateController.IsStarted );
			Assert.IsTrue( stateController.IsStopped );
			Assert.IsFalse( stateController.IsStartRequested );
			Assert.IsFalse( stateController.IsStopRequested );
		}

		private void AssertControllerStarted ( StateController stateController )
		{
			Assert.IsTrue( stateController.IsStarted );
			Assert.IsFalse( stateController.IsStopped );
			Assert.IsFalse( stateController.IsStartRequested );
			Assert.IsFalse( stateController.IsStopRequested );
		}
	}
}
