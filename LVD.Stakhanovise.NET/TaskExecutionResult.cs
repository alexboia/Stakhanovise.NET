using System;
using System.Collections.Generic;
using System.Text;
using LVD.Stakhanovise.NET.Model;

namespace LVD.Stakhanovise.NET
{
   public class TaskExecutionResult : IEquatable<TaskExecutionResult>
   {
      private QueuedTask mTask;

      private bool mExecutedSuccessfully;

      private QueuedTaskError mError;

      private bool mIsRecoverable;

      public TaskExecutionResult(QueuedTask task)
      {
         mTask = task ?? throw new ArgumentNullException(nameof(task));
         mExecutedSuccessfully = true;
      }

      public TaskExecutionResult(QueuedTask task, QueuedTaskError error, bool isRecoverable)
      {
         mTask = task ?? throw new ArgumentNullException(nameof(task));
         mError = error;
         mIsRecoverable = isRecoverable;
         mExecutedSuccessfully = false;
      }

      public bool Equals(TaskExecutionResult other)
      {
         return other != null &&
             Task.Equals(other.Task) &&
             ExecutedSuccessfully == other.ExecutedSuccessfully &&
             IsRecoverable == other.IsRecoverable &&
             object.Equals(Error, other.Error);
      }

      public override bool Equals(object obj)
      {
         return Equals(obj as TaskExecutionResult);
      }

      public override int GetHashCode()
      {
         int result = 1;

         result = result * 31 + mTask.GetHashCode();
         result = result * 31 + mExecutedSuccessfully.GetHashCode();
         result = result * 31 + mIsRecoverable.GetHashCode();

         if (mError != null)
            result = result * 31 + mError.GetHashCode();

         return result;
      }

      public QueuedTask Task => mTask;

      public bool ExecutedSuccessfully => mExecutedSuccessfully;

      public QueuedTaskError Error => mError;

      public bool IsRecoverable => mIsRecoverable;
   }
}
