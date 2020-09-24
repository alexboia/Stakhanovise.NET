using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface ITaskQueueTimingBeltSetup
	{
		ITaskQueueTimingBeltSetup UseTimingBelt ( ITaskQueueTimingBelt timingBelt );

		ITaskQueueTimingBeltSetup UseTimingBelt ( Func<ITaskQueueTimingBelt> timingBeltFactory );

		ITaskQueueTimingBeltSetup SetupBuiltInTimingBelt ( Action<IPostgreSqlTaskQueueTimingBeltSetup> setupAction );
	}
}
