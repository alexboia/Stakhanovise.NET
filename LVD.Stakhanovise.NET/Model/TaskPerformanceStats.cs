using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class TaskPerformanceStats : IEquatable<TaskPerformanceStats>
	{
		public TaskPerformanceStats ( string payloadType, long durationMilliseconds )
		{
			PayloadType = payloadType;
			DurationMilliseconds = durationMilliseconds;
		}

		public bool Equals(TaskPerformanceStats other)
		{
			return other != null &&
				string.Equals( PayloadType, other.PayloadType )
				&& DurationMilliseconds == other.DurationMilliseconds;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as TaskPerformanceStats );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + PayloadType.GetHashCode();
			result = result * 13 + DurationMilliseconds.GetHashCode();

			return result;
		}

		public string PayloadType { get; private set; }

		public long DurationMilliseconds { get; private set; }
	}
}
