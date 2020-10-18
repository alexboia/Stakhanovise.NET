using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET
{
	public interface ITimestampProvider
	{
		DateTimeOffset GetNow ();
	}
}
