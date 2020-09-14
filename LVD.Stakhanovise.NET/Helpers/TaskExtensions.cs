using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class TaskExtensions
	{
		public static Task<T> WithCleanup<T> ( this Task<T> task, Action<Task<T>> cleanup )
		{
			if ( task == null )
				throw new ArgumentNullException( nameof( task ) );

			if ( cleanup == null )
				throw new ArgumentNullException( nameof( cleanup ) );
			
			TaskCompletionSource<T> completion =
				new TaskCompletionSource<T>();

			task.ContinueWith( prev =>
			{
				cleanup.Invoke( prev );

				if ( task.IsCanceled )
					completion.SetCanceled();
				else if ( task.IsFaulted )
					completion.SetException( task.Exception );
				else
					completion.SetResult( task.Result );
			} );

			return completion.Task;
		}
	}
}
