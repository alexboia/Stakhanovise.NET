using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IStakhanoviseSetup
	{
		IStakhanoviseSetup SetupTimingBelt ( Action<ITaskQueueTimingBeltSetup> setupAction );

		IStakhanoviseSetup SetupPerformanceMonitorWriter ( Action<IPerformanceMonitorWriterSetup> setupAction );

		IStakhanoviseSetup SetupEngine ();
	}
}
