using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class QueuedTaskInfo
	{
		public Guid Id { get; set; }

		public string Type { get; set; }

		public string Source { get; set; }

		public object Payload { get; set; }

		public int Priority { get; set; }

		public DateTimeOffset LockedUntilTs { get; set; }
	}
}
