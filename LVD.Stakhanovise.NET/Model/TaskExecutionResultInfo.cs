using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class TaskExecutionResultInfo
	{
		private QueuedTaskError mError;

		private bool mIsRecoverable;

		private TaskExecutionStatus mStatus;

		private TaskExecutionResultInfo ( TaskExecutionStatus status, QueuedTaskError error, bool isRecoverable )
		{
			mStatus = status;
			mError = error;
			mIsRecoverable = isRecoverable;
		}

		public static TaskExecutionResultInfo Successful ()
		{
			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutedSuccessfully, 
				error: null, 
				isRecoverable: false );
		}

		public static TaskExecutionResultInfo Cancelled ()
		{
			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutionCancelled,
				error: null,
				isRecoverable: false );
		}

		public static TaskExecutionResultInfo ExecutedWithError ( QueuedTaskError error, bool isRecoverable )
		{
			if ( error == null )
				throw new ArgumentNullException( nameof( error ) );

			return new TaskExecutionResultInfo( TaskExecutionStatus.ExecutedWithError,
				error: error,
				isRecoverable: isRecoverable );
		}

		public bool ExecutedSuccessfully
			=> mStatus == TaskExecutionStatus.ExecutedSuccessfully;

		public bool ExecutionCancelled
			=> mStatus == TaskExecutionStatus.ExecutionCancelled;

		public QueuedTaskError Error
			=> mError;

		public bool IsRecoverable
			=> mIsRecoverable;
	}
}
