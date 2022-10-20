using System;

namespace LVD.Stakhanovise.NET.Model
{
	public static class IQueuedTaskExtensions
	{
		public static bool IsOfType<T>( this IQueuedTask task )
		{
			if ( task == null )
				throw new ArgumentNullException( nameof( task ) );

			return task.Payload != null
				&& task.Payload.GetType() == typeof( T );
		}
	}
}
