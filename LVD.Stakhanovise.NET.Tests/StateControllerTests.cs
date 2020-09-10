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

			await stateController.TryRequestStopASync( async () => { await Task.CompletedTask; } );
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
				await stateController.TryRequestStopASync( async () =>
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
				AssertControllerStarted( stateController );
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
					await controller.TryRequestStopASync( async () =>
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
