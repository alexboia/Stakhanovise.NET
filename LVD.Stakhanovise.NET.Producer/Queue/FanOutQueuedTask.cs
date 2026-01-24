using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Queue
{
	public class FanOutQueuedTask : IQueuedTask
	{
		private readonly IQueuedTask mPrimaryTask;

		private readonly IEnumerable<IQueuedTask> mAllTasks;

		private readonly IEnumerable<Exception> mErrors;

		public FanOutQueuedTask( IEnumerable<IQueuedTask> allTasks, IEnumerable<Exception> errors )
		{
			if (allTasks == null)
				throw new ArgumentNullException( nameof( allTasks ) );

			mAllTasks = allTasks.ToList();
			mPrimaryTask = mAllTasks.FirstOrDefault();
			mErrors = (errors ?? new List<Exception>()).ToList();

			if (mPrimaryTask == null)
				throw new ArgumentException( "At least one task must be provided for the fan-out result.", nameof( allTasks ) );
		}

		public Guid Id => mPrimaryTask.Id;

		public long LockHandleId => mPrimaryTask.LockHandleId;

		public string Type
		{
			get => mPrimaryTask.Type;
			set => throw new NotSupportedException( $"Setting {nameof( Source )} not supported on fan-out tasks" );
		}

		public string Source => mPrimaryTask.Source;

		public object Payload => mPrimaryTask.Payload;

		public int Priority
		{
			get => mPrimaryTask.Priority;
			set => throw new NotSupportedException( $"Setting {nameof( Priority )} not supported on fan-out tasks" );
		}

		public DateTimeOffset PostedAtTs => mPrimaryTask.PostedAtTs;

		public DateTimeOffset LockedUntilTs => mPrimaryTask.LockedUntilTs;

		public IEnumerable<Exception> Errors => mErrors;
	}
}
