using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Queue
{
	public class ClearForDequeueEventArgs : System.EventArgs
	{
		public ClearForDequeueEventArgs ( ClearForDequeReason reason )
			: base()
		{
			Reason = reason;
		}

		public ClearForDequeReason Reason { get; private set; }
	}
}
