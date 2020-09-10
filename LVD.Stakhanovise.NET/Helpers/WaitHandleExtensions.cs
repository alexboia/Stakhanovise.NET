using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class WaitHandleExtensions
	{
		private static void WaitForWaitHandleDelegate ( object state, bool timedOut )
		{
			TaskCompletionSource<bool> waitHandleSignaledCompletionSource =
				( TaskCompletionSource<bool> )state;

			waitHandleSignaledCompletionSource.TrySetResult( true );
		}

		public static Task<bool> ToTask ( this WaitHandle waitHandle, TimeSpan timeout )
		{
			TaskCompletionSource<bool> waitHandleSignaledCompletionSource
				= new TaskCompletionSource<bool>( waitHandle );

			RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject( waitHandle,
				callBack: WaitForWaitHandleDelegate,
				state: waitHandleSignaledCompletionSource,
				timeout: timeout,
				executeOnlyOnce: true );

			Task<bool> waitHandleSignaledCompletionTask =
				waitHandleSignaledCompletionSource.Task;

			waitHandleSignaledCompletionTask.ContinueWith( ( antecedent )
				 => registeredWaitHandle.Unregister( null ) );

			return waitHandleSignaledCompletionTask;
		}

		public static Task<bool> ToTask ( this WaitHandle waitHandle )
		{
			return waitHandle.ToTask( TimeSpan.FromMilliseconds( int.MaxValue ) );
		}
	}
}
