using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Concurrent;

namespace LVD.Stakhanovise.NET
{
	public class TaskExecutionContext : ITaskExecutionContext
	{
		private QueuedTask mTask;

		private ConcurrentDictionary<string, object> mContextData =
		   new ConcurrentDictionary<string, object>();

		private TaskExecutionResult mResult;

		public TaskExecutionContext ( QueuedTask task )
		{
			mTask = task
				?? throw new ArgumentNullException( nameof( task ) );
		}

		public void NotifyTaskCompleted ()
		{
			mResult = new TaskExecutionResult( mTask );
		}

		public void NotifyTaskErrored ( QueuedTaskError error, bool isRecoverable )
		{
			mResult = new TaskExecutionResult( mTask,
				error,
				isRecoverable );
		}

		public TValue Get<TValue> ( string key )
		{
			if ( string.IsNullOrEmpty( key ) )
				throw new ArgumentNullException( nameof( key ) );

			object value;
			if ( !mContextData.TryGetValue( key, out value ) )
				value = null;

			return value is TValue
				? ( TValue )value
				: default( TValue );
		}

		public void Set<TValue> ( string key, TValue value )
		{
			if ( string.IsNullOrEmpty( key ) )
				throw new ArgumentNullException( nameof( key ) );

			TValue current = Get<TValue>( key );
			mContextData.TryUpdate( key,
			   value,
			   current );
		}

		public QueuedTask Task => mTask;

		public TaskExecutionResult Result => mResult;

		public QueuedTaskStatus TaskStatus => mTask.Status;

		public bool HasResult => mResult != null;
	}
}
