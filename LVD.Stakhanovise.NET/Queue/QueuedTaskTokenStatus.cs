using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public enum QueuedTaskTokenStatus
	{
		Pending = 0x01,
		Active = 0x02,
		Completed = 0x03,
		Cancelled = 0x04
	}
}
