using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET
{
	public class UtcNowTimestampProvider : ITimestampProvider
	{
		public DateTimeOffset GetNow ()
		{
			return DateTimeOffset.UtcNow;
		}
	}
}
