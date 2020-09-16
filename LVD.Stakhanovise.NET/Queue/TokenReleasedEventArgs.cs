using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class TokenReleasedEventArgs : System.EventArgs
	{
		public TokenReleasedEventArgs ( Guid queuedTaskId )
			: base()
		{
			QueuedTaskId = queuedTaskId;
		}

		public Guid QueuedTaskId { get; private set; }
	}
}
