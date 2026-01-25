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
		}

		public Guid Id => mPrimaryTask?.Id ?? Guid.Empty;

		public long LockHandleId => mPrimaryTask?.LockHandleId ?? 0;

		public string Type
		{
			get => mPrimaryTask?.Type;
			set => throw new NotSupportedException( $"Setting {nameof( Source )} not supported on fan-out tasks" );
		}

		public string Source => mPrimaryTask?.Source;

		public object Payload => mPrimaryTask?.Payload;

		public int Priority
		{
			get => mPrimaryTask?.Priority ?? 0;
			set => throw new NotSupportedException( $"Setting {nameof( Priority )} not supported on fan-out tasks" );
		}

		public DateTimeOffset PostedAtTs => mPrimaryTask?.PostedAtTs ?? DateTimeOffset.MinValue;

		public DateTimeOffset LockedUntilTs => mPrimaryTask?.LockedUntilTs ?? DateTimeOffset.MinValue;

		public IEnumerable<IQueuedTask> AllTasks => mAllTasks;

		public IEnumerable<Exception> Errors => mErrors;
	}
}
